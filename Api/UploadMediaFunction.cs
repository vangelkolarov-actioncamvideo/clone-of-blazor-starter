using BlazorApp.Shared;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage;

namespace BlazorApp.Api
{
    public static class UploadMediaFunction
    {
        [FunctionName("UploadMedia")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req, ILogger log)
        {
            try
            {
                BlobContainerClient container = await GetCloudContainer();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                UploadedFile[] uploadedFiles = JsonConvert.DeserializeObject<UploadedFile[]>(requestBody);
                log.LogTrace("uploadedFiles.Count: " + uploadedFiles.Count());
                foreach (UploadedFile file in uploadedFiles)
                {
                    await UploadFileToCloud(container, file.FileName, file.ContentType, file.Size, file.FileContent);
                    log.LogTrace("Uploaded file: " + file.FileName);
                }

                return new OkObjectResult(uploadedFiles.Select(x => x.FileName));

            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error on UploadMedia function.");
                return new BadRequestObjectResult(ex.Message);
            }
        }

        static async Task<BlobContainerClient> GetCloudContainer()
        {
            var containerName = Environment.GetEnvironmentVariable("BlobStorageContainerName");
            var connectionString = Environment.GetEnvironmentVariable("BlobStorageStorageConnectionString");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (!blobContainerClient.Exists())
                await blobContainerClient.CreateAsync();

            return blobContainerClient;
        }

        static async Task<string> UploadFileToCloud(BlobContainerClient container, string filename, string contentType, long fileLength, byte[] fileContent)
        {
            BlobClient blobClient = container.GetBlobClient(filename);
            using (Stream stream = new MemoryStream(fileContent))
            {
                BlobUploadOptions options = new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = contentType },
                    Metadata = new Dictionary<string, string>
                    {
                        { "ApproxBlobCreatedDate", DateTime.UtcNow.ToString() },
                        { "OriginalFileSize", fileLength.ToString() }
                    },
                    TransferOptions = new StorageTransferOptions
                    {
                        InitialTransferLength = 1 * 1000 * 1000, // 1MB
                        InitialTransferSize = 1 * 1000 * 1000, // 1MB
                        MaximumConcurrency = 4,
                        MaximumTransferLength = 4 * 1000 * 1000, // 4MB
                        MaximumTransferSize = 4 * 1000 * 1000, // 4MB
                    }
                };

                await blobClient.UploadAsync(stream, options);
                //blobClient.SetHttpHeaders(new BlobHttpHeaders { ContentType = contentType });
                //blobClient.SetMetadata(new Dictionary<string, string>
                //{
                //    { "ApproxBlobCreatedDate", DateTime.UtcNow.ToString() },
                //    { "OriginalFileSize", fileLength.ToString() }
                //});
            }

            return blobClient.Name;
        }
    }
}
