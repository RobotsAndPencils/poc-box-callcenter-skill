using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.TranscribeService.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Linq;
using Amazon.Comprehend.Model;
using System.Text.RegularExpressions;
using Amazon.S3.Model;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace BoxTranscriptionLamda
{
    public class TranscribeFunction
    {
        public struct JobStatus
        {
            public const string IN_PROGRESS = "IN_PROGRESS";
            public const string COMPLETED = "COMPLETED";
            public const string FAILED = "FAILED";
        }

        public const int MAX_SPEAKER_LABELS = 2;//May need to be increased.
        private Regex blankPattern = new Regex("^\\W*$");
        private string TranscriptionFileName { get; set; }
        private static Configuration config = Configuration.GetInstance.Result;

        public static object DeserializeFromStream(Stream stream)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(stream))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize(jsonTextReader);
            }
        }

        private static string GetS3FileUrl(string bucket, string key)
        {
            return $"https://s3.amazonaws.com/{bucket}/{key}";
        }

        public async Task FunctionHandler(System.IO.Stream request, ILambdaContext context)
        {

            Console.WriteLine("======== API Event =========");
            string requestStr;
            using (StreamReader reader = new StreamReader(request))
            {
                requestStr = reader.ReadToEnd();
            }
            dynamic requestJson = JObject.Parse(requestStr);
            dynamic inputJson = JObject.Parse(requestJson.body.Value);
            Console.WriteLine("======== Request String =========");
            Console.WriteLine(requestStr);
            Console.WriteLine("======== Context =========");
            Console.WriteLine(JsonConvert.SerializeObject(context, Formatting.None));
            Console.WriteLine("======== Box Input (body) =========");
            Console.WriteLine(JsonConvert.SerializeObject(inputJson, Formatting.None));


            //var jobName = $"f{inputJson.source.id}_v{inputJson.source.file_version.id}";
            var jobName = $"f{inputJson.source.id}";

            // move file to S3 for processing (aws can not process using anything other than an S3 uir)
            var fileUrl = BoxHelper.getFileUrl(inputJson.source.id.Value, inputJson.token);
            Console.WriteLine($"FileUrl: {fileUrl}");
            string fileExt = Path.GetExtension(inputJson.source.name.Value).TrimStart('.');
            string fileName = $"{jobName}.{fileExt}";
            string mimeType = MimeMapping.GetMimeType(fileExt);

            if (fileExt.Equals("vsdx", StringComparison.CurrentCultureIgnoreCase)
                || fileExt.Equals("vdx", StringComparison.CurrentCultureIgnoreCase)) {
                await ZayoDemo.GenerateCards(inputJson);
                return;
            }

            if (fileExt.Equals("pdf", StringComparison.CurrentCultureIgnoreCase)
                || fileExt.Equals("pdf", StringComparison.CurrentCultureIgnoreCase)) {
                await AcmeDemo.GenerateCards(inputJson);
                return;
            }

            PutObjectResponse response = await UploadBoxFileToS3(fileUrl, config.S3BucketName, mimeType, fileName);
            Console.WriteLine("======== Put Object Response =========");
            Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.None));
            if (response.HttpStatusCode.CompareTo(HttpStatusCode.OK) != 0)
            {
                throw new Exception("Status code error");
            }

            Console.WriteLine("JobName: " + jobName);

            // Check for an existing job (maybe lambda was timed out and then re-run)
            var job = await GetTranscriptionJob(jobName);

            if (job == null || job?.TranscriptionJobStatus == null)
            {
                job = await StartTranscriptionJob(jobName, GetS3FileUrl(config.S3BucketName, fileName), fileExt);
            }

            switch (job?.TranscriptionJobStatus.Value)
            {
                case JobStatus.IN_PROGRESS:
                    job = await WaitForCompletion(jobName);
                    break;
                case JobStatus.FAILED:
                    Console.WriteLine("AWS Transcription job failed. Aborting");
                    return;
            }
            var result = await ProcessTranscriptionJob(job);
            CallCenterSkill.ProcessTranscriptionResults(ref result);
            DeleteObjectNonVersionedBucketAsync(fileName).Wait();
            await BoxHelper.GenerateCards(result, inputJson);
        }

        private async Task DeleteObjectNonVersionedBucketAsync(string key)
        {
            try
            {
                var deleteObjectRequest = new DeleteObjectRequest
                {
                    BucketName = config.S3BucketName,
                    Key = key
                };

                Console.WriteLine("Deleting an object");
                await config.S3Client.DeleteObjectAsync(deleteObjectRequest);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown encountered on server. Message:'{0}' when writing an object", e.Message);
            }
        }

        private async Task<PutObjectResponse> UploadBoxFileToS3(string url, string bucketName, string mimeType, string key)
        {
            WebRequest req = WebRequest.Create(url);
            WebResponse response = req.GetResponse();
            Stream responseStream = response.GetResponseStream();

            MemoryStream contentStream;

            using (var localStream = new MemoryStream())
            {
                byte[] buffer = new byte[2048]; // read in chunks of 2KB
                int bytesRead;
                while ((bytesRead = responseStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    localStream.Write(buffer, 0, bytesRead);
                }
                byte[] fileContent = localStream.ToArray();
                contentStream = new MemoryStream(fileContent);
            }


            PutObjectRequest request = new PutObjectRequest
            {
                BucketName = bucketName,
                ContentType = mimeType,
                Key = key,
                InputStream = contentStream
            };
            var result = await config.S3Client.PutObjectAsync(request);
            return result;
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
                var getTranscribeResponse = await config.AzTranscribeClient.GetTranscriptionJobAsync(new GetTranscriptionJobRequest()
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

        private async Task<TranscriptionJob> StartTranscriptionJob(string jobName, string mediaFileUri, string fileExt)
        {

            var transcriptionRequest = new StartTranscriptionJobRequest()
            {
                LanguageCode = "en-US",
                Media = new Media() { MediaFileUri = mediaFileUri },
                MediaFormat = fileExt,
                TranscriptionJobName = jobName,
                Settings = new Settings()
                {
                    MaxSpeakerLabels = MAX_SPEAKER_LABELS,
                    ShowSpeakerLabels = true
                },
            };
            Console.WriteLine($"Start Transcription job: {DateTime.Now}");
            var getTranscribeResponse = await config.AzTranscribeClient.StartTranscriptionJobAsync(transcriptionRequest);
            return getTranscribeResponse?.TranscriptionJob;
        }

        private async Task<SkillResult> ProcessTranscriptionJob(TranscriptionJob finishedJob)
        {
            var result = new SkillResult();

            if (finishedJob?.TranscriptionJobStatus.Value == JobStatus.FAILED)
            {
                Console.WriteLine($"Transcription job failed with reason:  {finishedJob.FailureReason}");
            }
            else if (finishedJob.TranscriptionJobStatus.Value == JobStatus.COMPLETED)
            {
                Console.WriteLine($"Transcription file located @: {finishedJob.Transcript.TranscriptFileUri}");

                var json = GetJobResultsForAnalsys(finishedJob.Transcript.TranscriptFileUri);

                JObject transcriptionResults = JObject.Parse(json);
                result = await ProcessTranscriptionResults(transcriptionResults);
            }

            return result;
        }

        private async Task<SkillResult> ProcessTranscriptionResults(JObject transcriptionResults)
        {
            var result = new SkillResult();

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
                if (segment.items.Length == 0) continue;

                result.duration = segment.end_time;

                if (!lastSpeaker.Equals(segment.speaker_label))
                {
                    // these lines do nothing the first iteration, but tie up last
                    // speaker result when the speaker is changing
                    currentSpeakerResult.text = speakerText.ToString();
                    speakerText = new StringBuilder();

                    // create new speaker result for new speaker - or first speaker on first iteration 
                    var idx = result.speakerLabels.IndexOf(segment.speaker_label);
                    if (idx == -1)
                    {
                        idx = result.speakerLabels.Count;
                        result.speakerLabels.Add(segment.speaker_label);
                        result.resultBySpeaker.Add(idx, new List<SpeakerResult>());
                    }

                    currentSpeakerResult = new SpeakerResult();
                    currentSpeakerResult.speaker = idx;
                    ConfigureTimeRange(ref currentSpeakerResult, segment);
                    lastSpeaker = segment.speaker_label;

                    result.resultBySpeaker[idx].Add(currentSpeakerResult);
                    result.resultByTime.Add(currentSpeakerResult);

                }
                else
                {
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

            // Call AWS Comprehend client to get sentiment for all speaker results
            List<int> keyList = new List<int>(result.resultBySpeaker.Keys);
            for (int keyIdx = 0; keyIdx < keyList.Count; keyIdx++)
            {
                var spkKey = keyList[keyIdx];
                for (int resultIdx = result.resultBySpeaker[spkKey].Count - 1; resultIdx >= 0; resultIdx--)
                {
                    if (!IsBlankText(result.resultBySpeaker[spkKey][resultIdx].text))
                    {
                        var speakerResult = result.resultBySpeaker[spkKey][resultIdx];
                        speakerResult.sentiment = await DetermineSentiment(result.resultBySpeaker[spkKey][resultIdx].text);
                        var topics = await DetermineTopic(result.resultBySpeaker[spkKey][resultIdx].text);
                        foreach (var topic in topics)
                        {
                            if (!result.topicLocations.ContainsKey(topic.Text))
                            {
                                result.topicLocations.Add(topic.Text, new List<SpeakerResult>());
                            }
                            result.topicLocations[topic.Text].Add(speakerResult);
                        }
                    }
                }
            }

            return result;
        }


        private bool IsBlankText(string text)
        {
            return blankPattern.Match(text).Success;
        }

        private void ConfigureTimeRange(ref SpeakerResult currentSpeakerResult, Segment segment)
        {
            foreach (var item in segment.items)
            {
                if (currentSpeakerResult.end == 0m) currentSpeakerResult.start = item.start_time;
                currentSpeakerResult.end = item.end_time;
            }
        }

        private static void LogSentimate(DetectSentimentResponse speakerSentimate, int speaker, string text)
        {
            Console.WriteLine($"Speaker: spk_{speaker}");
            Console.WriteLine($"text: {text}");
            Console.WriteLine($"sentiment: { speakerSentimate.Sentiment.Value }");
        }

        public async Task<DetectSentimentResponse> DetermineSentiment(string text)
        {
            // Call DetectKeyPhrases API        
            DetectSentimentRequest detectSentimentRequest = new DetectSentimentRequest()
            {
                Text = text,
                LanguageCode = "en"
            };
            DetectSentimentResponse detectSentimentResponse = await config.AzComprehendClient.DetectSentimentAsync(detectSentimentRequest);

            return detectSentimentResponse;
        }
        public async Task<List<KeyPhrase>> DetermineTopic(string text)
        {
            DetectKeyPhrasesRequest request = new DetectKeyPhrasesRequest()
            {
                LanguageCode = "en",
                Text = text
            };
            DetectKeyPhrasesResponse response = await config.AzComprehendClient.DetectKeyPhrasesAsync(request);
            var result = new List<KeyPhrase>();
            // Filter using a score of .9 as the threshold
            if (response != null && response.KeyPhrases.Count > 0)
            {
                foreach (var phrase in response.KeyPhrases)
                {
                    if (phrase.Score > -.7 && phrase.Text.Length > 4)
                    {
                        Console.WriteLine($"=== Phrase [{phrase.Text}] score: {phrase.Score}");
                        var str = phrase.Text;
                        phrase.Text = str.Remove(1).ToUpper() + str.Remove(0, 1).ToLower();
                        result.Add(phrase);
                    }
                }
            }
            return result;
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