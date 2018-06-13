using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;


namespace BoxTranscriptionPOC
{
    public class UploadToS3
    {
        AmazonS3Client _s3Client = null;

        public UploadToS3(string accessKeyId, string secretAccessKey, string serviceUrl)
        {
            AmazonS3Config s3Config = new AmazonS3Config
            {
                ServiceURL = serviceUrl
            };
            BasicAWSCredentials creds = new BasicAWSCredentials(accessKeyId, secretAccessKey);
            this._s3Client = new AmazonS3Client(creds, RegionEndpoint.APNortheast2);
        }


        public async Task UploadFile(string filePath, string s3Bucket, string newFileName)
        {
            //save in s3

            Console.WriteLine("Uploading");
            PutObjectRequest s3PutRequest = new PutObjectRequest
            {
                FilePath = filePath,
                BucketName = s3Bucket,
                CannedACL = S3CannedACL.PublicRead
            };

            //key - new file name
            if (!string.IsNullOrWhiteSpace(newFileName))
            {
                s3PutRequest.Key = newFileName;
            }

            s3PutRequest.Headers.Expires = new DateTime(2020, 1, 1);

            try
            {
                PutObjectResponse s3PutResponse = await this._s3Client.PutObjectAsync(s3PutRequest);

               
                //var fileTransferUtility =
                //     new TransferUtility(_s3Client);
                //// Option 1. Upload a file. The file name is used as the object key name.
                //await fileTransferUtility.UploadAsync(filePath, s3Bucket, newFileName);
                Console.WriteLine("Upload 1 completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}

