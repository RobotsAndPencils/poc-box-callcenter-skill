using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.S3;
using Box.V2;
using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;

using Newtonsoft.Json.Linq;
using Box.V2.Managers;
using Box.V2.Converter;
using Box.V2.Models;
using Amazon.S3.Model;
using Amazon.S3.Transfer;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace FileUploadwebHookLambda
{
    public class Functions
    {
        const string primaryKey = "";
        const string secondaryKey = "";
        public const string REGION_ENVIRONMENT_VARIABLE_NAME = "AWS_REGION";

        private const string S3_BUCKET_NAME = "transcriptions-poc-test";
        //TODO: put in env var.

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

        //public static readonly string _aws_Region =  System.Environment.GetEnvironmentVariable(REGION_ENVIRONMENT_VARIABLE_NAME);

        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
      
        }

        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        { 
            //TODO: Put box config in env var.
            //var boxConfig = System.Environment.GetEnvironmentVariable("BOX_CONFIG");
            var session = new BoxJWTAuth(BoxConfig.CreateFromJsonString(_boxConfig));
            var serviceAccountClient = session.AdminClient(session.AdminToken());



            var user = await serviceAccountClient.UsersManager.GetCurrentUserInformationAsync();
            context.Logger.LogLine(user.Name);

            var timestamp = request.Headers.First((header) => {
                return header.Key.ToUpper() == "BOX-DELIVERY-TIMESTAMP";
            });

            var primarySignature = request.Headers.First((header) => {
                return header.Key.ToUpper() == "BOX-SIGNATURE-PRIMARY";
            });

            var secondarySignature = request.Headers.First((header) => {
                return header.Key.ToUpper() == "BOX-SIGNATURE-SECONDARY";
            });

            var isWebhookValid = BoxWebhooksManager.VerifyWebhook(timestamp.Value, primarySignature.Value,
                secondarySignature.Value, request.Body, primaryKey, secondaryKey);

            context.Logger.LogLine($"Is webhook valid: {isWebhookValid}");
            context.Logger.LogLine("Parsing body...");
            if (isWebhookValid)
            {
                var message = JObject.Parse(request.Body);
                context.Logger.LogLine(message.ToString());
                JToken source;
                if (message.TryGetValue("source", out source))
                {
                    context.Logger.LogLine(source.ToString());
                    var converter = new BoxJsonConverter();
                    var incomingFile = converter.Parse<BoxFile>(source.ToString());
                    context.Logger.LogLine(incomingFile.Id);
                    context.Logger.LogLine(incomingFile.Name);
                    await ProcessIncomingFiles(incomingFile, serviceAccountClient);
                    context.Logger.LogLine($"Moved file: {incomingFile.Name}");
                }
                else
                {
                    throw new Exception("Couldn't find a source field in the webhook JSON");
                }
            }

            var response = new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
            };

            return response;
        }

        public async static Task ProcessIncomingFiles(BoxFile incomingFile, BoxClient boxClient)
        {

            if (incomingFile != null) {
                try
                {
                    var fileTransferUtility =  new TransferUtility(new AmazonS3Client());
                        // 1. Put object-specify only key name for the new object.
                        var putRequest1 = new PutObjectRequest
                        {
                            BucketName = S3_BUCKET_NAME,
                            Key = incomingFile.Name,
                            ContentType = "audio/x-wav",
                             
                        };
                    using (var fileStream = await boxClient.FilesManager.DownloadStreamAsync(incomingFile.Id))
                    {
                     
                        await fileTransferUtility.UploadAsync(fileStream,
                                                   S3_BUCKET_NAME, incomingFile.Name);
                    }
                    Console.WriteLine("Upload 3 completed");
                    

                }
                catch (AmazonS3Exception e)
                {
                    Console.WriteLine(
                            "Error encountered ***. Message:'{0}' when writing an object"
                            , e.Message);
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        "Unknown encountered on server. Message:'{0}' when writing an object"
                        , e.Message);
                }
            }
        }
    }
}