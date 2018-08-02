using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BoxTranscriptionLamda
{
    public static class BoxHelper
    {
        private static string BOX_API_ENDPOINT = System.Environment.GetEnvironmentVariable("boxApiEndpoint");
        private enum SkillType { timeline, keyword, transcript };

        public static string getFileUrl (string id, dynamic token) {
            return $"{BOX_API_ENDPOINT}/files/{id}/content?access_token={token.read.access_token}";
        }
        public static async Task GenerateCards(SkillResult result, dynamic boxBody)
        {
            var config = new BoxConfig(string.Empty, string.Empty, new Uri("http://boxsdk"));
            var session = new OAuthSession(boxBody.token.write.access_token.Value, string.Empty, 3600, "bearer");
            var client = new BoxClient(config, session);

            if (client == null)
            {
                throw new Exception("Unable to create box client");
            }

            Console.WriteLine("Created box client using write token");

            var cards = new List<Dictionary<string, object>>
            {
                GeneateTopicsKeywordCard(result, boxBody, client),
                GeneateScriptAdherenceKeywordCard(result, boxBody, client),
                GeneateTranscriptCard(result, boxBody, client)
            };
            //cards.AddRange(GeneateSentimentTimelineCards(result, boxBody, client));

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

        private static List<Dictionary<string, object>> GeneateSentimentTimelineCards(SkillResult result, dynamic boxBody, BoxClient client)
        {
            var card = GetSkillCardTemplate(SkillType.transcript, boxBody, "Transcript", result.duration);
            throw new NotImplementedException();
        }

        private static Dictionary<string, object> GeneateTranscriptCard(SkillResult result, dynamic boxBody, BoxClient client)
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

        public static Dictionary<string, object> GeneateScriptAdherenceKeywordCard(SkillResult result, dynamic boxBody, BoxClient client)
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
        public static Dictionary<string, object> GeneateTopicsKeywordCard(SkillResult result, dynamic boxBody, BoxClient client)
        {
            var card = GetSkillCardTemplate(SkillType.keyword, "TopicCard", boxBody, "Topics", result.duration);
            Console.WriteLine("Start entry loop");
            for (int i = 0; i < 20 && i<result.topics.Count; i++) {
                Console.WriteLine($"Create entry for: {result.topics[i]}");
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", result.topics[i] },
                    { "appears", new List<Dictionary<string, object>>() }
                };

                Console.WriteLine("Start location loop");
                foreach (var speakerResult in result.wordLocations[result.topics[i]]) {
                    Console.WriteLine($"Create location for: {speakerResult.start}, {speakerResult.end}");
                    var location = new Dictionary<string, object>() {
                        { "start", speakerResult.start },
                        { "end", speakerResult.end }
                    };
                    ((List<Dictionary<string, object>>)entry["appears"]).Add(location);
                }
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            Console.WriteLine("== WordLocations ==");
            Console.WriteLine(JsonConvert.SerializeObject(result.wordLocations, Formatting.None));

            Console.WriteLine("== Card ==");
            Console.WriteLine(JsonConvert.SerializeObject(card, Formatting.None));

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

