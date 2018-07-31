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

        public static string getFileUrl (string id, dynamic token) {
            return $"{BOX_API_ENDPOINT}/files/{id}/content?access_token={token.read.access_token}";
        }
        public static async Task GenerateCards(decimal duration, Dictionary<string, List<SpeakerResult>> results, dynamic boxBody)
        {
            var config = new BoxConfig(string.Empty, string.Empty, new Uri("http://boxsdk"));
            var session = new OAuthSession(boxBody.token.write.access_token.Value, string.Empty, 3600, "bearer");
            var client = new BoxClient(config, session);


            Console.WriteLine("Created box client using write token");

            var cards = new List<Dictionary<string, object>>
            {
                GeneateTopicsKeywordCard(duration, results, boxBody, client)
            };
            var skillsMetadata = new Dictionary<string, object>(){
                { "cards", cards }
            };

            try {
                await client.MetadataManager.CreateFileMetadataAsync(boxBody.source.id.Value, skillsMetadata, "global", "boxSkillsCards");
                Console.WriteLine("Created metadata");
            } catch (Exception e) {
                Console.WriteLine("Exception creating metadata. Trying update");
                Console.WriteLine(e);
                Console.WriteLine("== Exception Detail ==");
                Console.WriteLine(JsonConvert.SerializeObject(e, Formatting.None));

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
                    Console.WriteLine("== Exception Detail ==");
                    Console.WriteLine(JsonConvert.SerializeObject(e2, Formatting.None));
                    return;
                }
            }
        }

        //Run through results, grouping words and saving the locations in the media file. Create card with top 20
        //words more than 5 characters. 
        // TODO: should have common word list to ignore instead of <5 chars
        // TODO: should calculate proximity to find phrases that appear together
        public static Dictionary<string, object> GeneateTopicsKeywordCard(decimal duration, Dictionary<string, List<SpeakerResult>> results, dynamic boxBody, BoxClient client)
        {
            Console.WriteLine("== GeneateTopicsKeywordCard ==");
            //create dictionary of words to array of locations. Can then sort by array lengths
            var wordLocations = new Dictionary<string, List<SpeakerResult>>();

            Console.WriteLine("Building work locations dictionary");
            foreach (var speaker in results)
            {
                foreach (var result in results[speaker.Key])
                {
                    var text = Regex.Replace(result.text, @"\[\.,!?]", "");
                    text = Regex.Replace(text, @"\[ ]{2,}", " ");
                    foreach (string word in text.Split(' '))
                    {
                        if (word.Length > 5)
                        {
                            if (!wordLocations.ContainsKey(word))
                            {
                                wordLocations.Add(word, new List<SpeakerResult>());
                            }
                            wordLocations[word].Add(result);
                        }
                    }
                }
            }
            Console.WriteLine("Sorting words by word location occurances");
            List<string> words = new List<string>(wordLocations.Keys);
            words.Sort(delegate (string wordA, string wordB)
            {
                return wordLocations[wordB].Count - wordLocations[wordA].Count;
            });


            var card = GetKeywordCardTemplate();
            Console.WriteLine("Assign top level properties");
            card["id"] = boxBody.id;
            ((Dictionary<string, object>)card["skill"])["id"] = boxBody.skill.id;
            card["duration"] = duration;

            Console.WriteLine("Start entry loop");
            for (int i = 0; i < 20 && i<words.Count; i++) {
                Console.WriteLine($"Create entry for: {words[i]}");
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", words[i] },
                    { "appears", new List<Dictionary<string, object>>() }
                };

                Console.WriteLine("Start location loop");
                foreach (var result in wordLocations[words[i]]) {
                    Console.WriteLine($"Create location for: {result.start}, {result.end}");
                    var location = new Dictionary<string, object>() {
                        { "start", result.start },
                        { "end", result.end }
                    };
                    ((List<Dictionary<string, object>>)entry["appears"]).Add(location);
                }
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            Console.WriteLine("== WorkLocations ==");
            Console.WriteLine(JsonConvert.SerializeObject(wordLocations, Formatting.None));

            Console.WriteLine("== Card ==");
            Console.WriteLine(JsonConvert.SerializeObject(card, Formatting.None));

            return card;
        }
        private static Dictionary<string, object> GetKeywordCardTemplate()
        {
            var template = new Dictionary<string, object>() {
                { "type", "skill_card" },
                { "skill_card_type", "keyword" },
                { "skill", new Dictionary<string, object>() {
                        { "type", "service" },
                        { "id", "INJECTED" }
                }},
                { "invocation", new Dictionary<string, object>() {
                        { "type", "skill_invocation" },
                        { "id", "INJECTED" }
                }},
                { "skill_card_title", new Dictionary<string, object>() {
                        { "message", "Topics" }
                }},
                { "duration", 0 },
                { "entries",  new List<Dictionary<string, object>>() }
            };

            return template;
        }

    }

}

