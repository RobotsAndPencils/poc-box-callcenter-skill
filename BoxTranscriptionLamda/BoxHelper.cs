using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoxTranscriptionLamda
{
    public static class BoxHelper
    {
        private enum SkillType { timeline, keyword, transcript };
        private static Configuration config = Configuration.GetInstance.Result;

        public static string getFileUrl (string id, dynamic token) {
            return $"{config.BoxApiEndpoint}/files/{id}/content?access_token={token.read.access_token}";
        }
        public static async Task GenerateCards(SkillResult result, dynamic boxBody)
        {
            var boxConfig = new BoxConfig(string.Empty, string.Empty, new Uri(config.BoxApiUrl));
            var session = new OAuthSession(boxBody.token.write.access_token.Value, string.Empty, 3600, "bearer");
            var client = new BoxClient(boxConfig, session);

            if (client == null)
            {
                throw new Exception("Unable to create box client");
            }

            Console.WriteLine("======== BoxHelper Result =========");
            Console.WriteLine(JsonConvert.SerializeObject(result, Formatting.None));

            var cards = new List<Dictionary<string, object>>
            {
                GeneateScoreKeywordCard(result, boxBody),
                GeneateTopicsKeywordCard(result, boxBody),
                GeneateScriptAdherenceKeywordCard(result, boxBody),
                GeneateTranscriptCard(result, boxBody)
            };
            cards.AddRange(GeneateSentimentTimelineCards(result, boxBody));

            Console.WriteLine("======== Cards =========");
            Console.WriteLine(JsonConvert.SerializeObject(cards, Formatting.None));

            var skillsMetadata = new Dictionary<string, object>(){
                { "cards", cards }
            };

            try {
                await client.MetadataManager.CreateFileMetadataAsync(boxBody.source.id.Value, skillsMetadata, "global", "boxSkillsCards");
                Console.WriteLine("Created metadata");
            } catch (Exception e) {
                Console.WriteLine("Exception creating metadata. Trying update");
                Console.WriteLine(e);
                BoxMetadataUpdate updateObj = new BoxMetadataUpdate
                {
                    Op = MetadataUpdateOp.replace,
                    Path = "/cards",
                    Value = cards
                };
                try
                {
                    await client.MetadataManager.UpdateFileMetadataAsync(boxBody.source.id.Value, new List<BoxMetadataUpdate>() { updateObj }, "global", "boxSkillsCards");
                } catch (Exception e2) {
                    Console.WriteLine("Exception updating metadata. giving up");
                    Console.WriteLine(e2);
                    return;
                }
                Console.WriteLine("Successfully updated metadata");
            }
        }

        private static Dictionary<string, object> GeneateScoreKeywordCard(SkillResult result, dynamic boxBody)
        {
            var card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Support Score", result.duration);

            var entry = new Dictionary<string, object>() {
                { "type", "text" },
                { "text", $"Value: {result.supportScore}" }               
            };
            ((List<Dictionary<string, object>>)card["entries"]).Add(entry);

            if (result.supportScore <= 0m) {
                entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", "Followup: Negative" }
                };
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            } else if (result.supportScore > 4m) {
                entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", "Followup: Positive" }               
                };
                 ((List<Dictionary<string, object>>) card["entries"]).Add(entry);
            }
        
            return card;
        }

        private static List<Dictionary<string, object>> GeneateSentimentTimelineCards(SkillResult result, dynamic boxBody)
        {
            List<Dictionary<string, object>> cards = new List<Dictionary<string, object>>();

            foreach (var speaker in result.resultsBySpeakerSentiment.Keys) {
                var card = GetSkillCardTemplate(SkillType.timeline, boxBody, $"{result.speakerLabels[speaker]} Sentiment", result.duration);
                foreach (var sentValue in result.resultsBySpeakerSentiment[speaker].Keys) {
                    var entry = new Dictionary<string, object>() {
                        { "type", "text" },
                        { "text", sentValue },
                        { "appears", new List<Dictionary<string, object>>() }
                    };

                    foreach (var speakerResult in result.resultsBySpeakerSentiment[speaker][sentValue])
                        {
                            var location = new Dictionary<string, object>() {
                            { "start", speakerResult.start },
                            { "end", speakerResult.end }
                        };
                        ((List<Dictionary<string, object>>)entry["appears"]).Add(location);
                    }
                    ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
                }

                cards.Add(card);             
            }
            return cards;
        }

        private static Dictionary<string, object> GeneateTranscriptCard(SkillResult result, dynamic boxBody)
        {
            var card = GetSkillCardTemplate(SkillType.transcript, boxBody, "Transcript", result.duration);
            foreach (var speakerResult in result.resultByTime)
            {
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", $"[{result.speakerLabels[speakerResult.speaker]}]  {speakerResult.text}" },
                    { "appears", new List<Dictionary<string, object>>() {
                        new Dictionary<string, object>() {
                            { "start", speakerResult.start },
                            { "end", speakerResult.end }
                        }
                    } }
                };

                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            return card;
        }

        public static Dictionary<string, object> GeneateScriptAdherenceKeywordCard(SkillResult result, dynamic boxBody)
        {
            var card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Script Adherence", result.duration);

            foreach (var phraseKey in result.scriptChecks.Keys)
            {
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", $"{phraseKey}: {result.scriptChecks[phraseKey]}" },
                    { "appears", new List<Dictionary<string, object>>() }
                };

                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            return card;

        }




        //Run through results, grouping words and saving the locations in the media file. Create card with top 20
        //words more than 5 characters. 
        // TODO: should have common word list to ignore instead of <5 chars
        // TODO: should calculate proximity to find phrases that appear together
        public static Dictionary<string, object> GeneateTopicsKeywordCard(SkillResult result, dynamic boxBody)
        {
            var card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Topics", result.duration);
            var topics = new List<string>(result.topicLocations.Keys);
            var count = 0;

            topics.Sort(delegate (string a, string b)
            {
                return result.topicLocations[a].Count.CompareTo(result.topicLocations[b].Count);
            });

            foreach (var topic in topics) {
                if (count++ == 20) break;
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", topic },
                    { "appears", new List<Dictionary<string, object>>() }
                };

                foreach (var speakerResult in result.topicLocations[topic]) {
                    var location = new Dictionary<string, object>() {
                        { "start", speakerResult.start },
                        { "end", speakerResult.end }
                    };
                    ((List<Dictionary<string, object>>)entry["appears"]).Add(location);
                }
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }

            return card;
        }

        private static Dictionary<string, object> GetSkillCardTemplate(SkillType type, dynamic boxBody, string title, decimal duration)
        {
            var template = new Dictionary<string, object>() {
                { "type", "skill_card" },
                { "skill_card_type", type.ToString() },
                { "skill", new Dictionary<string, object>() {
                        { "type", "service" },
                        { "id", $"{title.Replace(" ","")}_{boxBody.id.Value}" }
                }},
                { "invocation", new Dictionary<string, object>() {
                        { "type", "skill_invocation" },
                        { "id", $"I{boxBody.id.Value}" }
                }},
                { "skill_card_title", new Dictionary<string, object>() {
                        { "message", title }
                }},
                { "duration", duration },
                { "entries",  new List<Dictionary<string, object>>() }
            };

            return template;
        }
    }

}

