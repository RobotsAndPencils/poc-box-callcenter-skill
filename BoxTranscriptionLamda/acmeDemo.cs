using Box.V2;
using Box.V2.Auth;
using Box.V2.Config;
using Box.V2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// This is a demo for Acme Brick. They currently scan invoices and run workflows
// but current software is out of date. Suggesting replacing with box with skill
// to call out to aws textract, then kick off workflows using K2
// 
// This Demo will expect the sample pdf files they gave us and then generate hardcoded
// skill cards with vendor and invoice metadata

namespace BoxTranscriptionLamda {
    public static class AcmeDemo {
        private enum SkillType { timeline, keyword, transcript };
        private static Configuration config = Configuration.GetInstance.Result;
        private static Random random = new Random();
        public static string getFileUrl(string id, dynamic token) {
            return $"{config.BoxApiEndpoint}/files/{id}/content?access_token={token.read.access_token}";
        }
        public static async Task GenerateCards(dynamic boxBody) {
            var boxConfig = new BoxConfig(string.Empty, string.Empty, new Uri(config.BoxApiUrl));
            var session = new OAuthSession(boxBody.token.write.access_token.Value, string.Empty, 3600, "bearer");
            var client = new BoxClient(boxConfig, session);

            if (client == null) {
                throw new Exception("Unable to create box client");
            }

            Console.WriteLine("======== AcmeDemo Processing started =========");

            var cards = GenerateAcmeDemoCards(boxBody);

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
                BoxMetadataUpdate updateObj = new BoxMetadataUpdate {
                    Op = MetadataUpdateOp.replace,
                    Path = "/cards",
                    Value = cards
                };
                try {
                    await client.MetadataManager.UpdateFileMetadataAsync(boxBody.source.id.Value, new List<BoxMetadataUpdate>() { updateObj }, "global", "boxSkillsCards");
                } catch (Exception e2) {
                    Console.WriteLine("Exception updating metadata. giving up");
                    Console.WriteLine(e2);
                    return;
                }
                Console.WriteLine("Successfully updated metadata");
            }
        }


        private static Dictionary<string, object> GetSkillCardTemplate(SkillType type, dynamic boxBody, string title, decimal duration) {
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
        public static List<Dictionary<string, object>> GenerateAcmeDemoCards(dynamic boxBody) {
            var filename = boxBody.source.name.Value;
            dynamic data = LoadJObject(filename);

            var cards = new List<Dictionary<string, object>>();

            var card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Company Information", 0);
            dynamic c = data.company;
            cards.Add(AddBasicEntries(card, new string[]
                {
                $"Company Name: {c.companyName.Value}",
                $"Company Phone: {c.companyPhone.Value}",
                $"Company Website: {c.companyWebsite.Value}",
                $"Account Number: {c.accountNumber.Value}"
                }
            ));
            dynamic i = data.invoice;
            card = GetSkillCardTemplate(SkillType.keyword, boxBody, "Invoice", 0);
            cards.Add(AddBasicEntries(card, new string[]
                {
                    $"Date: {i.invoiceDate.Value}",
                    $"Number: {i.invoiceNumber.Value}",
                    $"Previous Balance: {i.previousBalance.Value}",
                    $"Adjustments / Credits: {i.adjustmentsCredits.Value}",
                    $"New Charges: {i.newCharges.Value}",
                    $"Total Amount Due: {i.totalAmountDue.Value}",
                }
            ));

            return cards;
        }
        private static Dictionary<string, object> AddBasicEntries(Dictionary<string, object> card, String[] entryStrings) {
            foreach (var str in entryStrings) {
                var entry = new Dictionary<string, object>() {
                    { "type", "text" },
                    { "text", str }
                };
                ((List<Dictionary<string, object>>)card["entries"]).Add(entry);
            }
            return card;
        }

        public static JObject LoadJObject(string name) {
            Console.WriteLine($"Searching for Mock data for invoice file: {name}");
            string strData = LoadJson(name);
            if (strData?.Length == 0) {
                strData = "{\"company\":{\"companyName\":\"Sprocket Inc\",\"companyPhone\":\"215-555-1212\",\"companyWebsite\":\"sprocket.com\",\"accountNumber\":\"1932091239\"},\"invoice\":{\"invoiceDate\":\"2018-12-01\",\"invoiceNumber\":\"19983210\",\"previousBalance\":308.30,\"adjustmentsCredits\":-10.0,\"newCharges\":210.0,\"totalAmountDue\":508.30}}";
            }
            return JObject.Parse(strData);
        }
        public static string LoadJson(string name) {
            switch (name) {
                case "0823-000809792.pdf": 
                    return "{\"company\":{\"companyName\":\"Republic Services\",\"companyPhone\":\"601-939-2221\",\"companyWebsite\":\"RepublicServices.com/Support\",\"accountNumber\":\"3-0823-0008437\"},\"invoice\":{\"invoiceDate\":\"2018-08-31\",\"invoiceNumber\":\"0823-000809792\",\"previousBalance\":706.0,\"adjustmentsCredits\":-271.0,\"newCharges\":606.0,\"totalAmountDue\":1041.0}}";
                case "3086062694.pdf":
                    return "{\"company\":{\"companyName\":\"AmeriGas\",\"companyPhone\":\"601-939-1171\",\"companyWebsite\":\"www.amerigas.com\",\"accountNumber\":\"202467441\"},\"invoice\":{\"invoiceDate\":\"2019-01-12\",\"invoiceNumber\":\"3086062694\",\"previousBalance\":226.45,\"adjustmentsCredits\":0.0,\"newCharges\":222.25,\"totalAmountDue\":222.25}}";
                case "796193.pdf":
                    return "{\"company\":{\"companyName\":\"International Plastics\",\"companyPhone\":\"864-297-8000\",\"companyWebsite\":\"interPlas.com\",\"accountNumber\":\"153955\"},\"invoice\":{\"invoiceDate\":\"2018-12-17\",\"invoiceNumber\":\"796193\",\"previousBalance\":0.0,\"adjustmentsCredits\":0.0,\"newCharges\":1296.0,\"totalAmountDue\":1296.0}}";
                case "797120.pdf":
                    return "{\"company\":{\"companyName\":\"International Plastics\",\"companyPhone\":\"864-297-8000\",\"companyWebsite\":\"interPlas.com\",\"accountNumber\":\"153955\"},\"invoice\":{\"invoiceDate\":\"2018-12-28\",\"invoiceNumber\":\"797120\",\"previousBalance\":0.0,\"adjustmentsCredits\":0.0,\"newCharges\":108.0,\"totalAmountDue\":108.0}}";
                default:
                    Console.WriteLine($"Failed to find invoice match for: {name}");
                    return "";
            }
        }
    }
}


