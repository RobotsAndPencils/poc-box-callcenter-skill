using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AWSTranscriptionLamda
{
    public class BoxHelper
    {
        //TODO: MOVE TO ENV VAR as recommend by best practices.
        private static readonly string _boxConfig = "{" +
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

        public static async Task UploadTranscriptionBytesToBox(string url, string transcriptionFileName)
        {
            var trancribedBytes = GetFinishedTranscriptionFileBytes(url);

            IBoxConfig config = null;

            using (var webClient = new WebClient())
            {

                Console.WriteLine("create box config");

                config = BoxConfig.CreateFromJsonString(_boxConfig);

                var boxJWT = new BoxJWTAuth(config);
                // Create admin client
                var adminToken = boxJWT.AdminToken();
                var client = boxJWT.AdminClient(adminToken);

                Console.WriteLine($"Upload file {transcriptionFileName} to folder ID: {BOX_FOLDER_ID}");

                BoxFile newFile;
                using (var stream = new MemoryStream(trancribedBytes))
                {

                    var preflightRequest = new BoxPreflightCheckRequest
                    {
                        Name = transcriptionFileName,
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
                                Name = transcriptionFileName,
                                Parent = new BoxRequestEntity() { Id = BOX_FOLDER_ID }
                            };

                            newFile = await client.FilesManager.UploadAsync(req, stream);
                            Console.WriteLine($"New File Id: {newFile.Id}, {newFile.ExpiringEmbedLink}");

                        }
                        else
                        {
                            Console.WriteLine($"Pre-check failed");
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
        private static byte[] GetFinishedTranscriptionFileBytes(string url)
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


    }
    
}
