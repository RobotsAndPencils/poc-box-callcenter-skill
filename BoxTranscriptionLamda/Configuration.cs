using System;
using System.IO;
using System.Threading.Tasks;
using Amazon;
using Amazon.Comprehend;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.TranscribeService;
using Newtonsoft.Json.Linq;

namespace BoxTranscriptionLamda
{
    public class Configuration
    {
        public string S3Region { get; set; }
        public string S3BucketName { get; set; }
        public string S3ConfigKey { get; set; }
        public IAmazonS3 S3Client { get; }
        public AmazonTranscribeServiceClient AzTranscribeClient { get; }
        public AmazonComprehendClient AzComprehendClient { get; }
        public string BoxApiEndpoint { get; }
        public string BoxApiUrl { get; set; }
        public JObject ScriptAdherence { get; set; }
        public JObject SentimentImages { get; set; } //map positive, neutral, negative to image url
        public JObject PeopleImages { get; set; }  //map customer/support to arrays of image urls

        public Configuration()
        {
            this.S3Region = System.Environment.GetEnvironmentVariable("awsRegion");
            this.S3BucketName = System.Environment.GetEnvironmentVariable("s3BucketName");
            this.S3ConfigKey = System.Environment.GetEnvironmentVariable("s3ConfigKey");
            this.BoxApiEndpoint = System.Environment.GetEnvironmentVariable("boxApiEndpoint");

            var regionEndpoint = RegionEndpoint.GetBySystemName(S3Region);
            this.S3Client = new AmazonS3Client(regionEndpoint);
            this.AzTranscribeClient = new AmazonTranscribeServiceClient(regionEndpoint);
            this.AzComprehendClient = new AmazonComprehendClient(regionEndpoint);
        }

        public static Task<Configuration> GetInstance { get; } = CreateSingleton();

        private static async Task<Configuration> CreateSingleton()
        {
            var instance = new Configuration();
            await instance.InitializeAsync();
            return instance;
        }

        public async Task InitializeAsync () {
            string env = await GetS3FileContent(S3BucketName, S3ConfigKey);
            dynamic envJson = JObject.Parse(env);
            // Initialize json sourced properties
            this.BoxApiUrl = envJson.box.apiUrl.Value;
            this.ScriptAdherence = envJson.skillCards.scriptAdherence;
            this.SentimentImages = envJson.images.sentiment;
            this.PeopleImages = envJson.images.people;
        } 

        private async Task<string> GetS3FileContent(string bucket, string key)
        {
            string responseBody = "";

            try
            {
                GetObjectRequest request = new GetObjectRequest
                {
                    BucketName = bucket,
                    Key = key
                };
                using (GetObjectResponse response = await S3Client.GetObjectAsync(request))
                using (Stream responseStream = response.ResponseStream)
                using (StreamReader reader = new StreamReader(responseStream))
                {
                    string title = response.Metadata["x-amz-meta-title"]; // Assume you have "title" as medata added to the object.
                    string contentType = response.Headers["Content-Type"];
                    Console.WriteLine("Object metadata, Title: {0}", title);
                    Console.WriteLine("Content type: {0}", contentType);

                    responseBody = reader.ReadToEnd(); // Now you process the response body.
                }
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered ***. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            return responseBody;
        }
    }

}
