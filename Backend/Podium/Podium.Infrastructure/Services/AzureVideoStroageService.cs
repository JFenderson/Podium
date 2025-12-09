using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    public class AzureVideoStorageService : IVideoStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureVideoStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string missing.");

            var containerName = configuration["AzureStorage:VideoContainerName"]
                ?? "podium-videos";

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            _containerClient.CreateIfNotExists();
        }

        public Task<string> GenerateUploadUrlAsync(string fileName, string contentType, int expirationMinutes = 60)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-2), // clock skew buffer
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
                ContentType = contentType,
                Protocol = SasProtocol.Https
            };

            // "Write" and "Create" permissions are needed for uploading
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasUri.ToString());
        }

        public Task<string> GetVideoUrlAsync(string fileName, int expirationMinutes = 120)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);

            // Check if we need a SAS token (private) or just the URL (public)
            // Assuming private for student privacy:
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerClient.Name,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-2),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
                Protocol = SasProtocol.Https
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasUri.ToString());
        }

        public async Task DeleteVideoAsync(string fileName)
        {
            var blobClient = _containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}