using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BoxTranscriptionTest
{
    [TestClass]
    public class ConfigurationTest
    {
        [TestInitialize()]
        public void Startup()
        {
            TestHelper.ConfigureEnvironment();
        }

        [TestMethod]
        public void TestGetInstance()
        {
            var config = BoxTranscriptionLamda.Configuration.GetInstance.Result;
            Assert.IsNotNull(config);
        }

        [TestMethod]
        public void TestClassBasicProperties()
        {
            var config = BoxTranscriptionLamda.Configuration.GetInstance.Result;
            Assert.IsNotNull(config.AzComprehendClient);
            Assert.IsNotNull(config.AzTranscribeClient);
            Assert.IsNotNull(config.S3Client);
            Assert.IsNotNull(config.S3BucketName);
            Assert.IsNotNull(config.S3ConfigKey);
            Assert.IsNotNull(config.S3Region);
        }

        [TestMethod]
        public void TestClassJsonProperties()
        {
            var config = BoxTranscriptionLamda.Configuration.GetInstance.Result;
            Assert.IsNotNull(config.BoxApiUrl);
            Assert.IsNotNull(config.ScriptAdherence);
        }

    }


}
