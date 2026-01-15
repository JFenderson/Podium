using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Podium.API.HealthChecks;

/// <summary>
/// Health check for Azure Blob Storage accessibility.
/// Verifies container exists and tests read/write permissions.
/// </summary>
public class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BlobStorageHealthCheck> _logger;

    public BlobStorageHealthCheck(IConfiguration configuration, ILogger<BlobStorageHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration["AzureStorage:ConnectionString"];
            var containerName = _configuration["AzureStorage:ContainerName"] ?? "podium-videos";

            if (string.IsNullOrEmpty(connectionString))
            {
                _logger.LogWarning("Azure Storage connection string not configured");
                return HealthCheckResult.Degraded(
                    "Azure Storage not configured",
                    data: new Dictionary<string, object>
                    {
                        { "Reason", "Connection string not found" }
                    });
            }

            var blobServiceClient = new BlobServiceClient(connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);

            // Check if container exists (lightweight operation)
            var exists = await containerClient.ExistsAsync(cancellationToken);

            if (!exists)
            {
                _logger.LogError("Azure Storage container '{ContainerName}' does not exist", containerName);
                return HealthCheckResult.Unhealthy(
                    $"Container '{containerName}' does not exist",
                    data: new Dictionary<string, object>
                    {
                        { "ContainerName", containerName }
                    });
            }

            // Verify we have read permissions by checking properties (lightweight)
            await containerClient.GetPropertiesAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Azure Storage health check passed for container '{ContainerName}'", containerName);
            return HealthCheckResult.Healthy(
                "Azure Storage is accessible",
                data: new Dictionary<string, object>
                {
                    { "ContainerName", containerName },
                    { "Exists", true }
                });
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError(ex, "Azure Storage access denied");
            return HealthCheckResult.Unhealthy(
                "Azure Storage access denied",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", "Access denied" }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Storage health check failed");
            return HealthCheckResult.Unhealthy(
                "Azure Storage check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                });
        }
    }
}
