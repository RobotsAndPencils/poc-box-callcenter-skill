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
    public static class ZayoDemo
    {
        private enum SkillType { timeline, keyword, transcript };
        private static Configuration config = Configuration.GetInstance.Result;
        private static Random random = new Random();
        public static string getFileUrl(string id, dynamic token)
        {
            return $"{config.BoxApiEndpoint}/files/{id}/content?access_token={token.read.access_token}";
        }
        public static async Task GenerateCards(dynamic boxBody)
        {
            var boxConfig = new BoxConfig(string.Empty, string.Empty, new Uri(config.BoxApiUrl));
            var session = new OAuthSession(boxBody.token.write.access_token.Value, string.Empty, 3600, "bearer");
            var client = new BoxClient(boxConfig, session);

            if (client == null)
            {
                throw new Exception("Unable to create box client");
            }

            Console.WriteLine("======== ZayoDemo Processing started =========");

            var cards = GenerateZayoDemoCards(boxBody);

            Console.WriteLine("======== Cards =========");
            Console.WriteLine(JsonConvert.SerializeObject(cards, Formatting.None));

            var skillsMetadata = new Dictionary<string, object>(){
                { "cards", cards }
            };

            try
            {
                await client.MetadataManager.CreateFileMetadataAsync(boxBody.source.id.Value, skillsMetadata, "global", "boxSkillsCards");
                Console.WriteLine("Created metadata");
            }
            catch (Exception e)
            {
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
                }
                catch (Exception e2)
                {
                    Console.WriteLine("Exception updating metadata. giving up");
                    Console.WriteLine(e2);
                    return;
                }
                Console.WriteLine("Successfully updated metadata");
            }
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
        private static List<Dictionary<string, object>> GenerateZayoDemoCards(dynamic boxBody)
        {
            var cards = new List<Dictionary<string, object>>();

            var card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Project Data", 0);
            cards.Add(AddBasicEntries(card, new string[]
                {
                "SO/CP: 327026 / 027896",
                "Customer: Sabey Intergate Exchange LLC",
                "CID: EYTX\087956\\ZYO",
                "ISO: 0053335",
                "Product Type: Ethernet"
                }
            ));
            card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Fiber Span Dark Fiber ID", 0);
            cards.Add(AddBasicEntries(card, new string[]
                {
                    "F13E-0000034: Est. Distance - TBD, Est. Loss 1310 ~ TBD dB / 1550 ~ TBD dB",
                    "F13E-0000035: Est. Distance - TBD, Est. Loss 1310 ~ TBD dB / 1550 ~ TBD dB",
                    "F13E-0000036: Est. Distance - TBD, Est. Loss 1310 ~ TBD dB / 1550 ~ TBD dB"
                }
            ));
            card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Buildings", 0);
            cards.Add(AddBasicEntries(card, new string[]
                {
                "EVRTWARG / WA-117: 11781 Harbour Reach Dr., Mukilteo, WA",
                "1321 Colby Ave, Everett, WA",
                "EVRTWARG / WA-9PA: 900 Pacific Ave., Everett, WA",
                "BRIRWA03 / WA-23B: 23631 Brier Rd., Brier, WA",
                "STTLWAWB / WESTIN: 2001 6th Ave., Seattle, WA"
                }
            ));
            card = GetSkillCardTemplate(SkillType.keyword, boxBody, "People", 0);
            cards.Add(AddBasicEntries(card, new string[]
                {
                "Project Manager: Scott Morrison",
                "Service Delivery Coord: Anthony San Lorenzo" +
                "Fiber Design Eng: Kris Boccio"
                }
            ));

            return cards;
        }
        private static Dictionary<string, object> AddBasicEntries(Dictionary<string, object> card, String[] entryStrings)
        {
            foreach (var str in entryStrings)
            {
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", str }
                };
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            return card;
        }
    }
}


