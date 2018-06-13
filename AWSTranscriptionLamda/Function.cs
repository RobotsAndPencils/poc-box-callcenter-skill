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

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AWSTranscriptionLamda
{
    public class Function
    {

        public const string REGION_ENVIRONMENT_VARIABLE_NAME = "AWS_REGION";
        public const int MAX_SPEAKER_LABELS = 3;

        private readonly string _boxConfig = "{" +
                "\"boxAppSettings\": {" +
                "\"clientID\": \"miud8az8sbwudb4b31mg5tp37t326d3q\"," +
                "\"clientSecret\": \"yGoDwCXa5HUX62EiAhUwKDD9lp4EcVRQ\"," +
                "\"appAuth\": {" +
                    "\"publicKeyID\": \"bzk94z8n\"," +
                    "\"privateKey\": \"" + @"-----BEGIN ENCRYPTED PRIVATE KEY-----\nMIIFDjBABgkqhkiG9w0BBQ0wMzAbBgkqhkiG9w0BBQwwDgQIBw2CBtNHCpoCAggA\nMBQGCCqGSIb3DQMHBAjzCrQDzSNBEwSCBMi/uzN+XY8tf6zAYD0F+P5x1BJK7Tah\n06mf5xUrK3ONQL0pGaiugzMEjs8jY+mDBB5I7quNPikwGmX3T5wUg+pIL3J7LiYA\nITbOaMQhvIcAAkrQMw93GcmLyuyr41OVlijHkFhpXe5kkjiGgnkSDmCroWq/Dj0C\n2te90r7+vpKWJbaffSpE/EBI4/hqkmQ2Xxo9y48M1I79ICLEiWdGbsDDbYGu+cj+\nlleQiYwZgxemXDSRgLhh9mncpHYUjQTfQOOIsJF8vvf2j70eluzhsZdIxv5/R0OC\nvOeXOjV0t7HcwyOKuWUnlRwb14wsrjQrrJMth6usdY8NGJAFY8JxLiCwmPweAOgK\nq9EGmrys5fbJmawQos7SsbIaB9agdThgWyeWKEDe0+C/a0YuR1TPzIajpoY5qI0g\nv7eXmDdyjJij790act9MSUcG1+RTj4T8xelUUzZLqe7aYddPN6Yl9JDmsKy1dj2Q\nSp09wKUQLVZw8+QuLcpIo4GA4kqe5ezvZ3Jj01Le+Cz7KhIzTQ9erv5KwmpUuykr\nxX7g6BTrVS/E4Mc+ZQFoMMuvps3daKCP8gLubMoMPCRylSMtA95eAyNE3vRyxF9j\n+SWuWg3VmFD23KmLTixpBKPY/Mz7QsxCbhawZ78y/N/8qkRPFzGSzUTHD0XMsyfo\nmw9/7O4PRUuW8sicFCWAix7ixNHowE4tOj0U+Y/u08bQmKOpCJbSpXKhLRu7pfUY\nAj+uUqowZgQIUYAlPIijOyi+53MhfxPFEx1BMakiBXOEgaEK0rPRX1CsEVnMgI6W\neseLuZK7Wqs/KwaDOr05cFDMN/wz9sIQIrJtitQ30K7dBpv4GruJtEmheDyCYDF5\npMXFROgibekbfnbsZab3XJoLMqdTrcI1zKPaeqYdAghIrJxyjqQjmXoRSa+6Mz6e\nd2AN7kcEIPKkNDSAxMmKhCDW89u3shCaGVMW5EaaqLdmpPsjLj20RzX5qB/vHLIK\n8vyu8Z5PNGgW9TC5AZMPFVQ0tnxx00MqUeU0W6DkSR3L4KAaovoxHJIZE0EyaRAN\nKpvm55pDiaIBVSqk4BLA1RqL0UglchWDv+KOay7ZQlxtROU4iW0lnczHLmMJ8iNL\nNW9BYZEZ7ulh3S4hldC0JFqpDtwZBXYRCKHG2mKaNXZeoZ181nIlPqsIP5od34mZ\nFrIZtsD+sgTTKd9IGWsAJ3FwwgTMztV+QLckBvppeJDYT+7rRDXjk09ZO+DJsx4B\nuK6lLH5F5CYDApXDlPIvbHRLGHjNdSSHnhuaxJOXExcV3s5Eh9Dr61JYncHYvb3p\nR8koad7KzGt6lIxEDqUPI5GRvEZsFv12WrFIi5xAAnSK7nLQVwIUGla78PBmqVUt\nsKADfi/wWJZEQwpU1u5E62HxK9yFl+M0fsK4x6AbHloOq4y0bdUS0Bkmj1/cg1uV\noMVD3PoLeRvVi9Fs7SKc+I5I6JJf1bs6/XkGWPK/k0IjR3AQCKSQaxj0n4xQd1aS\noGv3xI5IUedlOl+Ejvj4dEwkmQVS+3gdpkI5GI+nKdbhe+ScwP4fSd2PV06MCxbQ\nFM+sB/YLqDI+arQXICLEtTUTfT8TIU3amkjSvYj2+tdp/F+NgfJWvO75fkQ61lCG\nl9I=\n-----END ENCRYPTED PRIVATE KEY-----\n" + "\"," +
                    "\"passphrase\": \"daadec02b10b24d2fb3dba0955a04161\"" +
                "}" +
            "}," +
            "\"enterpriseID\": \"57419327\"" +
         "}";
        private const string BOX_FOLDER_ID = "50157209968";

        private string _boxConfigFileName => "57419327_box_config.json";
        private string AWS_Region { get; set; }
        private string AWS_BucketName { get; set; }
        private string TranscriptionFileNmae { get; set; }
        private IAmazonS3 S3Client { get; }

        private AmazonTranscribeServiceClient _amazonTranscribeServiceClient { get; }


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
                    TranscriptionFileNmae = Path.GetFileNameWithoutExtension(record.S3.Object.Key) + ".json";
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
                var bytes = GetFinishedTranscriptionFileBytes(finishedJob.Transcript.TranscriptFileUri);
                if (bytes != null)
                {
                    await UploadTranscriptionBytesToBox(bytes);
                }
                var json = GetJobResultsForAnalsys(finishedJob.Transcript.TranscriptFileUri);
                JObject transcriptionResults = JObject.Parse(json);
               // ProcessTranscriptionResults(transcriptionResults);
            }
        }

        private void ProcessTranscriptionResults(JObject transcriptionResults)
        {
            var segments = transcriptionResults["results"]["speaker_labels"]["segments"].ToObject<List<Segments>>();
            //var speakers = results.Value<JObject>("speaker_labels");
            //var segments = results.Value<List<Segments>>("segments");
            //var items = results.Value<List<TranscribeItems>>("items");
            //Console.WriteLine($"items: {items?.Count} segments: {segments.Count}");
            Console.WriteLine($"ssegments: {segments.Count}");
        }

        private string GetJobResultsForAnalsys(string transcriptFileUri)
        {
            using (var webClient = new WebClient())
            {
                return webClient.DownloadString(transcriptFileUri);
            }
        }

        private byte[] GetFinishedTranscriptionFileBytes(string url)
        {

            byte[] fileBytes = null;
            using (var webClient = new WebClient())
            {
                fileBytes = webClient.DownloadData(url);
                if (fileBytes == null)
                {
                    Console.WriteLine($"No bytes");
                }
                else
                {
                    Console.WriteLine($"file bytes found: {fileBytes.Length}");

                }
                return fileBytes;
            }

        }

        private async Task UploadTranscriptionBytesToBox(byte[] trancribedBytes)
        {

            IBoxConfig config = null;

            using (var webClient = new WebClient())
            {

                Console.WriteLine("create box config");

                config = BoxConfig.CreateFromJsonString(_boxConfig);

                var boxJWT = new BoxJWTAuth(config);
                // Create admin client
                var adminToken = boxJWT.AdminToken();
                var client = boxJWT.AdminClient(adminToken);

                Console.WriteLine($"Upload file {TranscriptionFileNmae} to folder ID: {BOX_FOLDER_ID}");

                BoxFile newFile;
                using (var stream = new MemoryStream(trancribedBytes))
                {

                    var preflightRequest = new BoxPreflightCheckRequest
                    {
                        Name = TranscriptionFileNmae,
                        Size = stream.Length,
                        Parent = new BoxRequestEntity
                        {
                            Id = BOX_FOLDER_ID
                        }
                    };
                    try
                    {
                        var preflightCheck = await client.FilesManager.PreflightCheck(preflightRequest);
                        if (preflightCheck.Success)
                        {
                            Console.WriteLine($"Can upload.");
                            BoxFileRequest req = new BoxFileRequest()
                            {
                                Name = TranscriptionFileNmae,
                                Parent = new BoxRequestEntity() { Id = BOX_FOLDER_ID }
                            };

                            newFile = await client.FilesManager.UploadAsync(req, stream);
                            Console.WriteLine($"New File Id: {newFile.Id}, {newFile.ExpiringEmbedLink}");

                        }
                        else
                        {
                            Console.WriteLine($"Pre-check fialed");
                        }
                    }
                    catch (BoxPreflightCheckConflictException<BoxFile> e)
                    {
                        // Grab the ID from the conflicting file to delete the file, upload a new version, append a number to make the file name unique, etc.
                        System.Console.WriteLine(e.ConflictingItem.Id);
                        using (SHA1 sha1 = SHA1.Create())
                        {
                            var fileSHA = sha1.ComputeHash(stream);
                            // You can optionally rename the file while uploading a new version.
                            // var fileUploaded = await client.FilesManager.UploadNewVersionAsync("ubuntu-no-gui.iso", e.ConflictingItem.Id, stream: toUpload, contentMD5: fileSHA);

                            var fileUploaded = await client.FilesManager.UploadNewVersionAsync(fileName: e.ConflictingItem.Name, fileId: e.ConflictingItem.Id, stream: stream, contentMD5: fileSHA);
                            System.Console.WriteLine(fileUploaded.FileVersion.Id);
                        }

                    }
                    catch (Exception ex) { Console.WriteLine(ex); }

                }


            }

        }

        private struct JobStatus
        {
            public const string IN_PROGRESS = "IN_PROGRESS";
            public const string COMPLETED = "COMPLETED";
            public const string FAILED = "FAILED";
        }


    }
}
