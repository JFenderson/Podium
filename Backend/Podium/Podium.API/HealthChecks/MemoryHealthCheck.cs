using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Podium.API.HealthChecks;

/// <summary>
/// Health check for system memory usage.
/// Returns degraded status if memory usage exceeds 80%.
/// </summary>
public class MemoryHealthCheck : IHealthCheck
{
    private readonly ILogger<MemoryHealthCheck> _logger;

    public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var allocatedMemoryBytes = GC.GetTotalMemory(forceFullCollection: false);
            var allocatedMemoryMB = allocatedMemoryBytes / 1024 / 1024;

            // Get total memory info (works on Windows and Linux)
            var gcInfo = GC.GetGCMemoryInfo();
            var totalMemoryBytes = gcInfo.TotalAvailableMemoryBytes;
            var totalMemoryMB = totalMemoryBytes / 1024 / 1024;

            var usagePercentage = totalMemoryBytes > 0 
                ? (double)allocatedMemoryBytes / totalMemoryBytes * 100 
                : 0;

            var data = new Dictionary<string, object>
            {
                { "AllocatedMemoryMB", allocatedMemoryMB },
                { "TotalMemoryMB", totalMemoryMB },
                { "UsagePercentage", Math.Round(usagePercentage, 2) },
                { "Gen0Collections", GC.CollectionCount(0) },
                { "Gen1Collections", GC.CollectionCount(1) },
                { "Gen2Collections", GC.CollectionCount(2) }
            };

            // Check if usage is critical (>80%)
            if (usagePercentage > 80)
            {
                _logger.LogWarning("High memory usage detected: {UsagePercentage}%", Math.Round(usagePercentage, 2));
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High memory usage: {Math.Round(usagePercentage, 2)}%",
                    data: data));
            }

            _logger.LogInformation("Memory health check passed. Usage: {UsagePercentage}%", Math.Round(usagePercentage, 2));
            return Task.FromResult(HealthCheckResult.Healthy(
                "Memory usage is normal",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Memory health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Memory check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }));
        }
    }
}
