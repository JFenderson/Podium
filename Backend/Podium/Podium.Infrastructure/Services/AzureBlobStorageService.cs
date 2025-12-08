using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    public class AzureBlobStorageService : IStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobStorageService(IConfiguration configuration)  // ✅ Changed
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string not configured in appsettings.json");

            _containerName = configuration["AzureStorage:ContainerName"]
                ?? "podium-uploads";  // Default container name

            _blobServiceClient = new BlobServiceClient(connectionString);

            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            };

            await blobClient.UploadAsync(fileStream, new BlobUploadOptions
            {
                HttpHeaders = blobHttpHeaders
            });

            return blobClient.Uri.AbsoluteUri;
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = GetBlobNameFromPath(filePath);
            var blobClient = containerClient.GetBlobClient(blobName);

            var response = await blobClient.DownloadAsync();
            return response.Value.Content;
        }

        public async Task DeleteFileAsync(string filePath)  // ✅ Changed from Task<bool>
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = GetBlobNameFromPath(filePath);
            var blobClient = containerClient.GetBlobClient(blobName);

            await blobClient.DeleteIfExistsAsync();
            // No return value needed
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = GetBlobNameFromPath(filePath);
            var blobClient = containerClient.GetBlobClient(blobName);

            return await blobClient.ExistsAsync();
        }


        public Task<string> GenerateSasToken(string filePath, int expirationMinutes = 60)  // ✅ Return Task<string>
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobName = GetBlobNameFromPath(filePath);
            var blobClient = containerClient.GetBlobClient(blobName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasToken.ToString());  // ✅ Wrap in Task
        }

        private string GetBlobNameFromPath(string filePath)
        {
            var uri = new Uri(filePath);
            return uri.Segments.Last();
        }
    }
}