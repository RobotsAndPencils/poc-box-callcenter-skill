using Box.V2.Config;
using Box.V2.Exceptions;
using Box.V2.JWTAuth;
using Box.V2.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BoxTranscriptionPOC
{
    public class ConfigreFolderForWebHook
    {
        public static async Task ExecuteMainAsync(string webHookUri)
        {
            using (FileStream fs = new FileStream("./boxConfig.json", FileMode.Open))
            {

                var boxJWT = new BoxJWTAuth(BoxConfig.CreateFromJsonFile(fs));
                var adminToken = boxJWT.AdminToken();
                var boxClient = boxJWT.AdminClient(adminToken);


                var webhookTriggers = new string[] { "FILE.UPLOADED" };

                string uploadsFolderId;
                try
                {

                    var uploadsFolder = await boxClient.FoldersManager.CreateAsync(new BoxFolderRequest
                    {
                        Parent = new BoxRequestEntity
                        {
                            Id = "0"
                        },
                        Name = "AudioFileUploads"
                    });
                    Console.WriteLine(uploadsFolder.Id);
                    uploadsFolderId = uploadsFolder.Id;
                }
                catch (BoxConflictException<BoxFolder> e)
                {
                    Console.WriteLine(e.ConflictingItems.First().Id);
                    uploadsFolderId = e.ConflictingItems.First().Id;
                }

                BoxWebhook webhook;
                try
                {
                    webhook = await boxClient.WebhooksManager.CreateWebhookAsync(new BoxWebhookRequest
                    {
                        Target = new BoxRequestEntity
                        {
                            Id = uploadsFolderId,
                            Type = BoxType.folder
                        },
                        Address = webHookUri,
                        Triggers = webhookTriggers
                    });
                }
                catch (BoxConflictException<BoxWebhook> wh)
                {
                    var webhooks = await boxClient.WebhooksManager.GetWebhooksAsync(autoPaginate: true);
                    webhook = webhooks.Entries.Find((hook) => {
                        return hook.Target.Id == uploadsFolderId;
                    });
                }
                if (webhook != null)
                {
                    Console.WriteLine(webhook.Id);
                }

              
               
            }
        }
    }
}
