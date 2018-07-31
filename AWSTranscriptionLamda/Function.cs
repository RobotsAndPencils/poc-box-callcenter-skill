using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Linq;
using Amazon.Comprehend.Model;
using Amazon.Comprehend;
using System.Text.RegularExpressions;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace BoxTranscriptionLamda
{
    public class Function
    {
        public struct JobStatus
        {
            public const string IN_PROGRESS = "IN_PROGRESS";
            public const string COMPLETED = "COMPLETED";
            public const string FAILED = "FAILED";
        }

        public class SpeakerResult {
            public decimal start;
            public decimal end;
            public string text;
            public DetectSentimentResponse sentiment;
        }

        public const string REGION_ENVIRONMENT_VARIABLE_NAME = "AWS_REGION";
        public const int MAX_SPEAKER_LABELS = 2;//May need to be increased.
        private Regex blankPattern = new Regex("^\\W*$");

        private string AWS_Region { get; set; }
        private string AWS_BucketName { get; set; }
        private string TranscriptionFileName { get; set; }
        private IAmazonS3 S3Client { get; }

        private AmazonTranscribeServiceClient _amazonTranscribeServiceClient { get; }
        private AmazonComprehendClient _comprehendClient { get; }
        private Dictionary<string, List<SpeakerResult>> results = new Dictionary<string, List<SpeakerResult>>();

        HashSet<string> SupportedImageTypes { get; } = new HashSet<string> { ".wav", ".mp3", ".mp3", ".flac" };

        /// <summary>
        /// Default constructor used by AWS Lambda to construct the function. Credentials and Region information will
        /// be set by the running Lambda environment.
        /// 
        /// This constructor will also search for the environment variable overriding the default minimum confidence level
        /// for label detection.
        /// </summary>
        public Function()
        {
            this.S3Client = new AmazonS3Client();
            this._amazonTranscribeServiceClient = new AmazonTranscribeServiceClient();
            this._comprehendClient = new AmazonComprehendClient();
            this.AWS_Region = System.Environment.GetEnvironmentVariable(REGION_ENVIRONMENT_VARIABLE_NAME);


        }

        /// <summary>
        /// Constructor used for testing which will pass in the already configured service clients.
        /// </summary>
        /// <param name="s3Client"></param>
        /// <param name="rekognitionClient"></param>
        /// <param name="minConfidence"></param>
        public Function(IAmazonS3 s3Client, string awsRegionName)
        {
            this.S3Client = s3Client;
            this.AWS_Region = awsRegionName;
        }

        /// <summary>
        /// A function for responding to S3 create events. It will determine if the object is an image and use Amazon Rekognition
        /// to detect labels and add the labels as tags on the S3 object.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task FunctionHandler(S3Event input, ILambdaContext context)
        {
            if (input?.Records != null)
            {
                foreach (var record in input.Records)//should only be one.
                {
                    var fileExtension = Path.GetExtension(record.S3.Object.Key);
                    if (!SupportedImageTypes.Contains(Path.GetExtension(record.S3.Object.Key)))
                    {
                        Console.WriteLine($"Object {record.S3.Bucket.Name}:{record.S3.Object.Key} is not a supported image type");
                        continue;
                    }

                    Console.WriteLine($"File {record.S3.Bucket.Name}:{record.S3.Object.Key}");
                    AWS_BucketName = record.S3.Bucket.Name;
                    TranscriptionFileName = Path.GetFileNameWithoutExtension(record.S3.Object.Key) + ".json";
                    var filePath = $"https://{record.S3.Bucket.Name}.s3.{AWS_Region}.amazonaws.com/{record.S3.Object.Key}";
                    Console.WriteLine($"File path in was: {filePath}");
                    var jobName = "Transcription" + record.S3.Object.Key;

                    var existingJob = await GetTranscriptionJob(jobName);
                    if (existingJob == null || existingJob?.TranscriptionJobStatus == null)
                    {

                        var newJob = await StartTranscriptionJob(jobName, filePath, fileExtension);
                        if (newJob != null)
                        {

                            string jobStatus = newJob?.TranscriptionJobStatus.Value;
                            if (jobStatus == JobStatus.IN_PROGRESS)
                            {
                                newJob = await WaitForCompletion(jobName);
                                await ProcessTranscriptionJob(newJob);
                            }
                        }

                    }
                    else
                    {
                        switch (existingJob?.TranscriptionJobStatus.Value)
                        {
                            case JobStatus.IN_PROGRESS:
                                existingJob = await WaitForCompletion(jobName);
                                await ProcessTranscriptionJob(existingJob);
                                break;
                            case JobStatus.COMPLETED:
                            case JobStatus.FAILED:
                            default:
                                await ProcessTranscriptionJob(existingJob);
                                break;
                        }
                    }

                }
            }
            return;
        }

        private async Task<TranscriptionJob> WaitForCompletion(string jobName)
        {
            var jobStatus = JobStatus.IN_PROGRESS;
            TranscriptionJob foundJob = null;
            Console.WriteLine($"Transcription job In progress");
            while (jobStatus == JobStatus.IN_PROGRESS)
            {

                Thread.Sleep(10000);//sleep for 20 seconds  this can take a while
                foundJob = await GetTranscriptionJob(jobName);
                if (foundJob == null)
                {
                    Console.WriteLine($"{jobName} not found");
                    break;
                }
                jobStatus = foundJob?.TranscriptionJobStatus.Value;


            }
            Console.WriteLine($"Transcription job Finished");

            return foundJob;
        }

        private async Task<TranscriptionJob> GetTranscriptionJob(string jobName)
        {
            TranscriptionJob job = null;
            try
            {
                var getTranscribeResponse = await this._amazonTranscribeServiceClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest()
                {
                    TranscriptionJobName = jobName
                });
                job = getTranscribeResponse?.TranscriptionJob;
            }
            catch (BadRequestException) { }//Do nothing, job not found
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return job;

        }

        private async Task<TranscriptionJob> StartTranscriptionJob(string jobName, string filePath, string fileExtension)
        {

            var transcriptionRequest = new StartTranscriptionJobRequest()
            {
                LanguageCode = "en-US",
                Media = new Media() { MediaFileUri = filePath },
                MediaFormat = fileExtension.TrimStart('.'),
                TranscriptionJobName = jobName,
                Settings = new Settings()
                {
                    MaxSpeakerLabels = MAX_SPEAKER_LABELS,
                    ShowSpeakerLabels = true
                },
            };
            Console.WriteLine($"Start Transcription job: {DateTime.Now}");
            var getTranscribeResponse = await this._amazonTranscribeServiceClient.StartTranscriptionJobAsync(transcriptionRequest);
            return getTranscribeResponse?.TranscriptionJob;
        }

        private async Task ProcessTranscriptionJob(TranscriptionJob finishedJob)
        {
            if (finishedJob?.TranscriptionJobStatus.Value == JobStatus.FAILED)
            {
                Console.WriteLine($"Transcription job failed with reason:  {finishedJob.FailureReason}");
            }
            else if (finishedJob.TranscriptionJobStatus.Value == JobStatus.COMPLETED)
            {
                Console.WriteLine($"Transcription file located @: {finishedJob.Transcript.TranscriptFileUri}");

               
                var json = GetJobResultsForAnalsys(finishedJob.Transcript.TranscriptFileUri);

                JObject transcriptionResults = JObject.Parse(json);
                await ProcessTranscriptionResults(transcriptionResults);
                //TODO: save all results, not just transcription

                var jsonResults = JsonConvert.SerializeObject(results);
                await BoxHelper.UploadTranscriptionBytesToBox(Encoding.UTF8.GetBytes(jsonResults), TranscriptionFileName);
            }
        }

        private async Task ProcessTranscriptionResults(JObject transcriptionResults)
        {
            StringBuilder speakerText = new StringBuilder();
            TranscribeAlternative alternative = null;

            var segments = transcriptionResults["results"]["speaker_labels"]["segments"].ToObject<List<Segment>>();
            var transciptionsItems = transcriptionResults["results"]["items"].ToObject<List<TranscribeItem>>();

            Console.WriteLine($"items: {transciptionsItems?.Count} segments: {segments.Count}");

            var speakerLabel = string.Empty;
            var lastSpeaker = "nobody";
            SpeakerResult currentSpeakerResult = new SpeakerResult();

            var itemIdx = 0;

            var ti = transciptionsItems;
            // sements have a begin and end, however the items contained in it also
            // have begin and ends. the range of the items have a 1 to 1 correlation to the 'pronunciation' transcription
            // item types. These also have ends which are outside the range of the segement strangely. So will be using segment to
            // get the speaker, then will create an inclusive range for all items under it using the being of first and end of last. 
            foreach (var segment in segments)
            {
                if (!lastSpeaker.Equals(segment.speaker_label)) {
                    currentSpeakerResult.text = speakerText.ToString();
                    speakerText = new StringBuilder();
                    currentSpeakerResult = new SpeakerResult();
                    ConfigureTimeRange(ref currentSpeakerResult, segment);
                    lastSpeaker = segment.speaker_label;


                    if (!results.ContainsKey(lastSpeaker)) {
                        results.Add(lastSpeaker, new List<SpeakerResult>());
                    }
                    results[lastSpeaker].Add(currentSpeakerResult);

                } else {
                    ConfigureTimeRange(ref currentSpeakerResult, segment);
                }

                for (; itemIdx < ti.Count 
                     && ((currentSpeakerResult.start <= ti[itemIdx].start_time && ti[itemIdx].end_time <= currentSpeakerResult.end)
                         || (ti[itemIdx].start_time == 0m))
                     ; itemIdx++)
                    {
                    alternative = ti[itemIdx].alternatives.First();
                    if (alternative.content.Equals("[SILENCE]"))
                    {
                        speakerText.Append(".");
                    }
                    else
                    {
                        speakerText.Append(alternative.content);
                    }
                    speakerText.Append(" ");
                }          

            }
            currentSpeakerResult.text = speakerText.ToString();


            Console.WriteLine("Full Results (Transcription + sentiment):");
            List<string> keyList = new List<string>(results.Keys);
            for (int keyIdx = 0; keyIdx < keyList.Count ; keyIdx++) {
                var spkKey = keyList[keyIdx];
                Console.WriteLine($"Speaker: {spkKey}");
                // this should be done in paralell
                for (int resultIdx = 0; resultIdx < results[spkKey].Count; resultIdx++) {
                    var speakerResult = results[spkKey][resultIdx];
                    if (!isBlankText(results[spkKey][resultIdx].text))
                    {
                        speakerResult.sentiment = await GenerateSentiment(results[spkKey][resultIdx].text);
                        LogSentimate(results[spkKey][resultIdx].sentiment, spkKey, results[spkKey][resultIdx].text);
                    }
                }
            }
        }


        private bool isBlankText (string text) {
            return blankPattern.Match(text).Success;
        }

        private void ConfigureTimeRange (ref SpeakerResult currentSpeakerResult, Segment segment) {
            foreach (var item in segment.items)
            {
                if (currentSpeakerResult.start == 0m) currentSpeakerResult.start = item.start_time;
                currentSpeakerResult.end = item.end_time;
            }
        }

        private static void LogSentimate(DetectSentimentResponse speakerSentimate, string speaker, string text)
        {
            Console.WriteLine($"Speaker: {speaker}");
            Console.WriteLine($"text: {text}");
            Console.WriteLine($"sentiment: { speakerSentimate.Sentiment.Value}");
        }

        public async Task<DetectSentimentResponse> GenerateSentiment(string text)
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
        private string GetJobResultsForAnalsys(string transcriptFileUri)
        {
            using (var webClient = new WebClient())
            {
                return webClient.DownloadString(transcriptFileUri);
            }
        }

    }

}
