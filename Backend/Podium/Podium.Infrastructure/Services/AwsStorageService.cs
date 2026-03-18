using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    public class AwsStorageService : IStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public AwsStorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["CloudflareR2:BucketName"]
                ?? configuration["AWS:BucketName"]
                ?? throw new InvalidOperationException("Storage bucket name not configured (CloudflareR2:BucketName).");
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var request = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                InputStream = fileStream,
                ContentType = contentType
            };
            await _s3Client.PutObjectAsync(request);
            return fileName;
        }

        public async Task<Stream> DownloadFileAsync(string filePath)
        {
            var response = await _s3Client.GetObjectAsync(_bucketName, filePath);
            return response.ResponseStream;
        }

        public async Task DeleteFileAsync(string filePath)
        {
            await _s3Client.DeleteObjectAsync(_bucketName, filePath);
        }

        public async Task<bool> FileExistsAsync(string filePath)
        {
            try
            {
                await _s3Client.GetObjectMetadataAsync(_bucketName, filePath);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public Task<string> GenerateSasToken(string filePath, int expirationMinutes = 60)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = filePath,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };
            return Task.FromResult(_s3Client.GetPreSignedURL(request));
        }
    }
}
