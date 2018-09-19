using System;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionTest
{
    public class TestHelper
    {
        public TestHelper()
        {
        }

        // TODO: Yeah, I don't know what I'm doing. Couldn't get environment settings to load
        // when configuring in project runtime default config or in project file as property
        public static void ConfigureEnvironment()
        {
            System.Environment.SetEnvironmentVariable("s3BucketName", "box-poc");
            System.Environment.SetEnvironmentVariable("s3ConfigKey", "config.json");
            System.Environment.SetEnvironmentVariable("boxApiEndpoint", "https://api.box.com/2.0");
            System.Environment.SetEnvironmentVariable("boxAuth", "not needed anymore I think");
            System.Environment.SetEnvironmentVariable("boxFolderId", "50899287342");
            System.Environment.SetEnvironmentVariable("awsRegion", "us-east-1");
        }

        public static JObject loadJObject (string name) {
            return JObject.Parse(loadJson(name));
        }
        public static string loadJson(string name)
        {
            return System.IO.File.ReadAllText($"testData/{name}.json");
        }
    }
}
