using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models; // For PublicAccessType
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Podium.Core.Interfaces;
using System;
using System.Threading.Tasks;

namespace Podium.Infrastructure.Services
{
    public class AzureVideoStorageService : IVideoStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private bool _isInitialized = false; // Flag to track initialization

        public AzureVideoStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("Azure Storage connection string missing.");

            _containerName = configuration["AzureStorage:VideoContainerName"]
                ?? "podium-videos";

            // LIGHTWEIGHT: Only create the client object. No network calls here.
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        // --- LAZY INITIALIZATION HELPER ---
        private async Task<BlobContainerClient> GetContainerClientAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            if (!_isInitialized)
            {
                // This is the network call that was crashing your constructor.
                // Now it only happens when you actually try to use video features.
                await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
                _isInitialized = true;
            }

            return containerClient;
        }

        public async Task<string> GenerateUploadUrlAsync(string fileName, string contentType, int expirationMinutes = 60)
        {
            // 1. Ensure container exists
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-2), // clock skew buffer
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
                ContentType = contentType,
                Protocol = SasProtocol.Https
            };

            // "Write" and "Create" permissions are needed for uploading
            sasBuilder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);

            // Note: GenerateSasUri requires the Storage Account Key (available in emulator/connection string)
            // If using Managed Identity in production, this logic changes slightly (User Delegation SAS).
            // For now (Emulator/Key), this works fine.
            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }
            else
            {
                throw new InvalidOperationException("Cannot generate SAS URI. Check storage connection string.");
            }
        }

        public async Task<string> GetVideoUrlAsync(string fileName, int expirationMinutes = 120)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(fileName);

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                Resource = "b",
                StartsOn = DateTimeOffset.UtcNow.AddMinutes(-2),
                ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(expirationMinutes),
                Protocol = SasProtocol.Https
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            if (blobClient.CanGenerateSasUri)
            {
                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }

            return blobClient.Uri.ToString(); // Fallback (likely won't work if private)
        }

        public async Task DeleteVideoAsync(string fileName)
        {
            var containerClient = await GetContainerClientAsync();
            var blobClient = containerClient.GetBlobClient(fileName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}