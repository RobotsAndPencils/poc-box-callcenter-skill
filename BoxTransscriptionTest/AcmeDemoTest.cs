using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionTest {
    [TestClass]
    public class AcmeDemoTest {
        private static dynamic mockBoxBody;

        [TestInitialize()]
        public void Startup() {
           mockBoxBody = TestHelper.loadJObject("mockBoxBody");
        }

        //Dependency: test data file should have a match for every defined script component
        [TestMethod]
        public void TestLoadInvoice797120() {
            //var result = TestHelper.loadJObject("goodScriptAdherence_Final").ToObject<BoxTranscriptionLamda.SkillResult>();
            mockBoxBody.source.id = "797120.pdf";

            var skillCard = BoxTranscriptionLamda.AcmeDemo.GenerateAcmeDemoCards(mockBoxBody);

            Console.WriteLine(JsonConvert.SerializeObject(skillCard, Formatting.None));
            Assert.IsNotNull(skillCard);
            Assert.AreEqual("Company Information", skillCard[0]["skill_card_title"]["message"]);

        }

    }


}
