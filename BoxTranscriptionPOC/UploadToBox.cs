using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Linq;
using System.Threading.Tasks;

namespace BoxTranscriptionPOC
{
    public class UploadToBox
    {
        public const string BOX_CONFIG_JSON = "./boxConfig.json";
        const long CHUNKED_UPLOAD_MINIMUM = 200000;
        public static async Task ExecuteMainAsync(string filePath, string fileName, string folderId)
        {
            using (FileStream fs = new FileStream(BOX_CONFIG_JSON, FileMode.Open))
            {
                var session = new BoxJWTAuth(BoxConfig.CreateFromJsonFile(fs));
                var client = session.AdminClient(session.AdminToken());
                BoxFile newBoxFile = null;

                var fullPath = string.Concat(filePath, "/", fileName);
                if (File.Exists(fullPath))
                {
                    var fileInfo = new FileInfo(fullPath);
                    var preflightRequest = new BoxPreflightCheckRequest
                    {
                        Name = fileName,
                        Size = fileInfo.Length,
                        Parent = new BoxRequestEntity
                        {
                            Id = folderId
                        }
                    };
                    using (FileStream toUpload = new FileStream(fullPath, FileMode.Open))
                    {
                        try
                        {
                            var preflightCheck = await client.FilesManager.PreflightCheck(preflightRequest);
                            //if (toUpload.Length < CHUNKED_UPLOAD_MINIMUM)
                            //{
                                using (SHA1 sha1 = SHA1.Create())
                                {
                                    var fileUploadRequest = new BoxFileRequest
                                    {
                                        Name = fileName,
                                        Parent = new BoxRequestEntity
                                        {
                                            Id = folderId
                                        }
                                    };
                                    var fileSHA = sha1.ComputeHash(toUpload);
                                    System.Console.WriteLine(fileSHA);
                                    newBoxFile = await client.FilesManager.UploadAsync(fileRequest: fileUploadRequest, stream: toUpload, contentMD5: fileSHA);
                                    Console.WriteLine("New Box File ID: " + newBoxFile?.Id);

                                }
                            //}
                            //else
                            //{
                            //    await client.FilesManager.UploadUsingSessionAsync(stream: toUpload, fileName: fileName, folderId: folderId);
                            //}
                        }
                        catch (BoxPreflightCheckConflictException<BoxFile> e)
                        {
                            //if (toUpload.Length < CHUNKED_UPLOAD_MINIMUM)
                            //{
                                using (SHA1 sha1 = SHA1.Create())
                                {
                                    var fileSHA = sha1.ComputeHash(toUpload);
                                    newBoxFile = await client.FilesManager.UploadNewVersionAsync(fileName: e.ConflictingItem.Name, fileId: e.ConflictingItem.Id, stream: toUpload, contentMD5: fileSHA);
                                }
                            //}
                            //else
                            //{
                            //    newBoxFile = await client.FilesManager.UploadNewVersionUsingSessionAsync(fileId: e.ConflictingItem.Id, stream: toUpload);
                            //}
                        }
                        Console.WriteLine("New Box File ID: " + newBoxFile?.Id ?? "No File");

                    }
                }
                else
                {
                    Console.WriteLine(fullPath + " Doesn't exist");
                }
            }

        }


    }
}
