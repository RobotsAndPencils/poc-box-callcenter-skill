using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionTest
{
    [TestClass]
    public class CallCenterSkillTest
    {
        [TestInitialize()]
        public void Startup()
        {
            TestHelper.ConfigureEnvironment();
        }

        //Dependency: test data file should have a match for every defined script component
        [TestMethod]
        public void TestFindsAllScriptComponents()
        {
            JObject jsonResult = TestHelper.loadJObject("goodScriptAdherence_CallCenterSkill_GenerateScriptAdherence");
            var result = jsonResult.ToObject<BoxTranscriptionLamda.SkillResult>();
            BoxTranscriptionLamda.CallCenterSkill.GenerateScriptAdherence(ref result);
            var count = 0;
            foreach (var pair in result.scriptChecks) {
                count += pair.Value ? 1 : 0;
            }
            Assert.AreEqual(count, result.scriptChecks.Count);
        }

        [TestMethod]
        public void TestFindsAllScriptComponentsTopLevel()
        {
            JObject jsonResult = TestHelper.loadJObject("goodScriptAdherence_CallCenterSkill_ProcessTranscriptionResults");
            var result = jsonResult.ToObject<BoxTranscriptionLamda.SkillResult>();
            BoxTranscriptionLamda.CallCenterSkill.ProcessTranscriptionResults(ref result);
            var count = 0;
            foreach (var pair in result.scriptChecks)
            {
                count += pair.Value ? 1 : 0;
            }
            Assert.AreEqual(count, result.scriptChecks.Count);
        }

    }


}
