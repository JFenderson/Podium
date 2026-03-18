using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    public class AwsVideoStorageService : IVideoStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucketName;

        public AwsVideoStorageService(IAmazonS3 s3Client, IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucketName = configuration["CloudflareR2:BucketName"]
                ?? configuration["AWS:BucketName"]
                ?? throw new InvalidOperationException("Video storage bucket name not configured (CloudflareR2:BucketName).");
        }

        public Task<string> GenerateUploadUrlAsync(string fileName, string contentType, int expirationMinutes = 60)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                Verb = HttpVerb.PUT,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                ContentType = contentType
            };

            // Add metadata if needed (e.g. x-amz-meta-original-name)
            // request.Metadata.Add("original-name", ...);

            string url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public Task<string> GetVideoUrlAsync(string fileName, int expirationMinutes = 120)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = fileName,
                Verb = HttpVerb.GET,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes)
            };

            string url = _s3Client.GetPreSignedURL(request);
            return Task.FromResult(url);
        }

        public async Task DeleteVideoAsync(string fileName)
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = fileName
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
        }
    }
}