using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;


namespace BoxTranscriptionPOC
{
    class Program
    {
        public const string ARG_UPLOAD_TO_BOX = "box";
        public const string ARG_CONFIGURE_WEBHOOK = "webhook";
        public const string ARG_UPLOAD_TO_S3 = "s3";
        public const string ARG_PARSE_TRANSCRIPTION_FILE = "parse";
        private static IConfiguration _configuration { get; set; }

        static  void Main(string[] args)
        {
            try
            {
                ExecuteMainAsync(args).Wait();
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
            }
            Console.WriteLine($"Finished: Press any key to exit:");
            Console.ReadKey();
        }
        public static async Task ExecuteMainAsync(string[] args)
        {
            var builder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json");

            _configuration = builder.Build();

            if (args != null) {
                string fileName = null;
                string filePath = null;

                foreach (var arg in args) {
                    switch (arg) {
                        case ARG_CONFIGURE_WEBHOOK:
                            var webHookUri = _configuration["boxWebHookUri"];
                            Console.WriteLine($"Start wire up webhook folder ID:{webHookUri}");
                            ConfigreFolderForWebHook.ExecuteMainAsync(webHookUri).Wait();
                            break;
                        case ARG_UPLOAD_TO_BOX:
                             fileName = _configuration["uploadFileName"];
                             filePath = _configuration["uploadFilePath"];
                            string boxFolderId = _configuration["boxUploadFolderID"];
                            Console.WriteLine($"Start file upload to box: path {filePath}/{fileName}, folder ID:{boxFolderId}");
                            try
                            {
                                UploadToBox.ExecuteMainAsync(filePath, fileName, boxFolderId).Wait();
                            }
                            catch (Exception ex) {
                                Console.WriteLine(ex.ToString());
                            }
                            break;
                        case ARG_UPLOAD_TO_S3:
                            //TODO: amazon didn't not like these keys in github.
                            //also unable to use them to connect anyway.
                            //var secretKey = _configuration["s3secretKey"];
                            //var publicKey = _configuration["s3publicKey"];
                            var s3ServiceUrl = _configuration["s3serviceUrl"];
                             fileName = _configuration["uploadFileName"];
                             filePath = _configuration["uploadFilePath"];
                            string bucketName = _configuration["s3bucketName"];
                            //Console.WriteLine($"Start file upload to s3: path {filePath}/{fileName}, Bucket Name:{bucketName}");

                            //var s3uploader = new UploadToS3(publicKey, secretKey, s3ServiceUrl);
                            //s3uploader.UploadFile(string.Concat(filePath, "\\", fileName), bucketName, fileName).Wait();
                            ////var comprehend = new AswComprehend(publicKey, secretKey);
                            //var sentiment = await comprehend.GenerateSentiment("Test Text");
                            //Console.WriteLine($"type: {sentiment?.Sentiment}, score {sentiment.SentimentScore}");
                            break;
                        case ARG_PARSE_TRANSCRIPTION_FILE:
                            TranscriptionParser.ParseFile("./transciption.json");
                            break;
                        default:
                            break;
                    }

                }

            }
         

        }


    }
}