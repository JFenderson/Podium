using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Podium.API.HealthChecks;

/// <summary>
/// Health check for available disk space.
/// Returns degraded status if available space is less than 10GB.
/// </summary>
public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly ILogger<DiskSpaceHealthCheck> _logger;
    private const long MinimumFreeBytesThreshold = 10L * 1024 * 1024 * 1024; // 10 GB

    public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drives = DriveInfo.GetDrives()
                .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                .ToList();

            if (!drives.Any())
            {
                _logger.LogWarning("No ready fixed drives found");
                return Task.FromResult(HealthCheckResult.Degraded(
                    "No drives available to check",
                    data: new Dictionary<string, object>
                    {
                        { "Reason", "No fixed drives ready" }
                    }));
            }

            // Check the system drive (where the application is running)
            var systemDrive = drives.FirstOrDefault(d => d.RootDirectory.FullName == Path.GetPathRoot(Directory.GetCurrentDirectory()));
            if (systemDrive == null)
            {
                systemDrive = drives.First(); // Fallback to first available drive
            }

            var freeSpaceGB = systemDrive.AvailableFreeSpace / 1024.0 / 1024.0 / 1024.0;
            var totalSpaceGB = systemDrive.TotalSize / 1024.0 / 1024.0 / 1024.0;
            var usedSpaceGB = totalSpaceGB - freeSpaceGB;
            var usagePercentage = (usedSpaceGB / totalSpaceGB) * 100;

            var data = new Dictionary<string, object>
            {
                { "DriveName", systemDrive.Name },
                { "FreeSpaceGB", Math.Round(freeSpaceGB, 2) },
                { "TotalSpaceGB", Math.Round(totalSpaceGB, 2) },
                { "UsedSpaceGB", Math.Round(usedSpaceGB, 2) },
                { "UsagePercentage", Math.Round(usagePercentage, 2) }
            };

            // Check if free space is below threshold
            if (systemDrive.AvailableFreeSpace < MinimumFreeBytesThreshold)
            {
                _logger.LogWarning("Low disk space on {DriveName}: {FreeSpaceGB} GB free", 
                    systemDrive.Name, Math.Round(freeSpaceGB, 2));
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {Math.Round(freeSpaceGB, 2)} GB free",
                    data: data));
            }

            _logger.LogInformation("Disk space health check passed. {FreeSpaceGB} GB free on {DriveName}", 
                Math.Round(freeSpaceGB, 2), systemDrive.Name);
            return Task.FromResult(HealthCheckResult.Healthy(
                "Sufficient disk space available",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disk space health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Disk space check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }));
        }
    }
}
