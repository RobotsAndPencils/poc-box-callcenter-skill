using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionTest
{
    [TestClass]
    public class BoxHelperTest
    {
        private static dynamic mockBoxBody = TestHelper.loadJObject("mockBoxBody");

        [TestInitialize()]
        public void Startup()
        {
            TestHelper.ConfigureEnvironment();
        }

        //Dependency: test data file should have a match for every defined script component
        [TestMethod]
        public void TestFindsAllScriptComponents()
        {
            var result = TestHelper.loadJObject("goodScriptAdherence_Final").ToObject<BoxTranscriptionLamda.SkillResult>();

            var skillCard = BoxTranscriptionLamda.BoxHelper.GeneateScriptAdherenceKeywordCard(result, mockBoxBody);          

            Assert.AreEqual(result.scriptChecks.Count, skillCard["entries"].Count);
            foreach (var entry in skillCard["entries"]) {
                Assert.IsTrue(entry["text"].ToLower().Contains("true"));
            }

        }

    }


}
