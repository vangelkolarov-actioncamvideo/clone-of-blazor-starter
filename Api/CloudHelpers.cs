using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlazorApp.Api
{
    public static class CloudHelpers
    {
        public static CloudTableClient GetTableClient()
        {
            var connectionString = Environment.GetEnvironmentVariable("BlobStorageStorageConnectionString");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

            CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

            return tableClient;
        }

        public static async Task<CloudTable> GetTable(CloudTableClient tableClient, string tableName)
        {
            CloudTable table = tableClient.GetTableReference(tableName);

            await table.CreateIfNotExistsAsync();

            return table;
        }

        public static List<MediaFileMetadata> GetMediaFileMetadata(string fileName)
        {
            //Brighton_ZipLeft-2USZ6T-23170403.mp4
            //EDEN_Swing-ZPVLUT-RSJYNU-15134424.mp4
            List<MediaFileMetadata> mediaFileMetadatas = new List<MediaFileMetadata>();
            int underscoreIndex = fileName.IndexOf("_");
            int[] dashIndexes = fileName.AllIndexesOf("-").ToArray();
            if (underscoreIndex > 0 && dashIndexes.Length > 1 && dashIndexes[0] > underscoreIndex)
            {
                string fileNameNoExtension = Path.GetFileNameWithoutExtension(fileName);
                for (int i = 1; i < dashIndexes.Length; i++)
                {
                    var mediaFileMetadata = new MediaFileMetadata { Filename = fileName };
                    mediaFileMetadata.Site = fileNameNoExtension[0..underscoreIndex];
                    mediaFileMetadata.Game = fileNameNoExtension[(underscoreIndex + 1)..dashIndexes[0]];
                    mediaFileMetadata.Code = fileNameNoExtension[(dashIndexes[i - 1] + 1)..dashIndexes[i]];
                    mediaFileMetadata.Index = fileNameNoExtension[(dashIndexes[dashIndexes.Length - 1] + 1)..];
                    mediaFileMetadatas.Add(mediaFileMetadata);
                }
            }
            return mediaFileMetadatas;
        }

        public static IEnumerable<int> AllIndexesOf(this string str, string searchstring)
        {
            int minIndex = str.IndexOf(searchstring);
            while (minIndex != -1)
            {
                yield return minIndex;
                minIndex = str.IndexOf(searchstring, minIndex + searchstring.Length);
            }
        }

        public static async Task<BlobContainerClient> GetCloudContainer()
        {
            var containerName = Environment.GetEnvironmentVariable("BlobStorageContainerName");
            var connectionString = Environment.GetEnvironmentVariable("BlobStorageStorageConnectionString");

            BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
            if (!blobContainerClient.Exists())
                await blobContainerClient.CreateAsync();

            return blobContainerClient;
        }

        public static async Task<string> UploadFileToCloud(BlobContainerClient container, string filename, string contentType, long fileLength, byte[] fileContent)
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
