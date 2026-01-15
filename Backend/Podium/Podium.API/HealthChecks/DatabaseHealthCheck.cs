using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Podium.Infrastructure.Data;
using System.Diagnostics;

namespace Podium.API.HealthChecks;

/// <summary>
/// Health check for SQL Server database connectivity and performance.
/// Returns degraded status if query execution time exceeds 500ms.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ApplicationDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Execute a simple query to check connectivity
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            stopwatch.Stop();
            var duration = stopwatch.ElapsedMilliseconds;

            if (!canConnect)
            {
                _logger.LogError("Database health check failed: Cannot connect to database");
                return HealthCheckResult.Unhealthy(
                    "Cannot connect to database",
                    data: new Dictionary<string, object>
                    {
                        { "DurationMs", duration }
                    });
            }

            // Check if query is slow (>500ms)
            if (duration > 500)
            {
                _logger.LogWarning("Database health check degraded: Query took {DurationMs}ms", duration);
                return HealthCheckResult.Degraded(
                    $"Database query is slow ({duration}ms)",
                    data: new Dictionary<string, object>
                    {
                        { "DurationMs", duration },
                        { "Threshold", 500 }
                    });
            }

            _logger.LogInformation("Database health check passed in {DurationMs}ms", duration);
            return HealthCheckResult.Healthy(
                "Database is responsive",
                data: new Dictionary<string, object>
                {
                    { "DurationMs", duration }
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with exception");
            return HealthCheckResult.Unhealthy(
                "Database check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                });
        }
    }
}
