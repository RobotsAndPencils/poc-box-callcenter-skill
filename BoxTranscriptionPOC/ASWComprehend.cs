using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using Amazon.Runtime;

namespace BoxTranscriptionPOC
{
    public class AswComprehend
    {
        AmazonComprehendClient _comprehendClient = null;

        public AswComprehend(string accessKeyId, string secretAccessKey)
        {
            BasicAWSCredentials creds = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            _comprehendClient = new AmazonComprehendClient(creds,Amazon.RegionEndpoint.APNortheast2);
        }
        public async  Task<DetectSentimentResponse> GenerateSentiment(string text)
        {


            // Call DetectKeyPhrases API
            Console.WriteLine("Calling DetectSentiment");
            DetectSentimentRequest detectSentimentRequest = new DetectSentimentRequest()
            {
                Text = text,
                LanguageCode = "en"
            };
            DetectSentimentResponse detectSentimentResponse = await _comprehendClient.DetectSentimentAsync(detectSentimentRequest);

            Console.WriteLine(detectSentimentResponse?.Sentiment);
            return detectSentimentResponse;
        }
    }
}
