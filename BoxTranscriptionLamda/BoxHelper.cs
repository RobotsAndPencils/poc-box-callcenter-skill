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
        private const string AWSKEY_BOX_AUTH = "boxAuth";
        private const string AWSKEY_BOX_FOLDER_ID = "boxFolderId";

        private static string boxConfig = System.Environment.GetEnvironmentVariable(AWSKEY_BOX_AUTH);
        private static string BOX_FOLDER_ID = System.Environment.GetEnvironmentVariable(AWSKEY_BOX_FOLDER_ID);

        public static async Task UploadTranscriptionBytesToBox(string url, string transcriptionFileName) {
            var buffer = GetFinishedTranscriptionFileBytes(url);
            await UploadTranscriptionBytesToBox(buffer, transcriptionFileName);
        }
        public static async Task UploadTranscriptionBytesToBox(byte[] buffer, string transcriptionFileName)
        {
            IBoxConfig config = null;

            using (var webClient = new WebClient())
            {

                Console.WriteLine("create box config");

                config = BoxConfig.CreateFromJsonString(boxConfig);

                var boxJWT = new BoxJWTAuth(config);
                // Create admin client
                var adminToken = boxJWT.AdminToken();
                var client = boxJWT.AdminClient(adminToken);

                Console.WriteLine($"Upload file {transcriptionFileName} to folder ID: {BOX_FOLDER_ID}");

                BoxFile newFile;
                using (var stream = new MemoryStream(buffer))
                {

                    var preflightRequest = new BoxPreflightCheckRequest
                    {
                        Name = transcriptionFileName,
                        //Size = stream.Length,
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
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                        throw ex;
                    }

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

