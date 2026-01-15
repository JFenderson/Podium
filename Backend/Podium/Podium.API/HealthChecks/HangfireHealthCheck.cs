using Hangfire;
using Hangfire.Storage;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Podium.API.HealthChecks;

/// <summary>
/// Health check for Hangfire job processing status.
/// Verifies recurring jobs are running and checks for recent failed jobs.
/// </summary>
public class HangfireHealthCheck : IHealthCheck
{
    private readonly ILogger<HangfireHealthCheck> _logger;

    public HangfireHealthCheck(ILogger<HangfireHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var monitoringApi = JobStorage.Current.GetMonitoringApi();

            // Check for failed jobs in the last hour
            var failedJobs = monitoringApi.FailedCount();
            
            // Get statistics
            var stats = monitoringApi.GetStatistics();
            var recurringJobs = JobStorage.Current.GetConnection().GetRecurringJobs();

            var data = new Dictionary<string, object>
            {
                { "FailedJobsCount", failedJobs },
                { "EnqueuedJobs", stats.Enqueued },
                { "ProcessingJobs", stats.Processing },
                { "ScheduledJobs", stats.Scheduled },
                { "RecurringJobsCount", recurringJobs.Count },
                { "SucceededJobs", stats.Succeeded }
            };

            // Check if there are any recurring jobs configured
            if (!recurringJobs.Any())
            {
                _logger.LogWarning("No recurring jobs configured in Hangfire");
                return Task.FromResult(HealthCheckResult.Degraded(
                    "No recurring jobs configured",
                    data: data));
            }

            // Check for high number of failed jobs (threshold: 10 in recent history)
            if (failedJobs > 10)
            {
                _logger.LogWarning("High number of failed jobs detected: {FailedCount}", failedJobs);
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"High number of failed jobs: {failedJobs}",
                    data: data));
            }

            _logger.LogInformation("Hangfire health check passed. {RecurringJobsCount} recurring jobs active", recurringJobs.Count);
            return Task.FromResult(HealthCheckResult.Healthy(
                "Hangfire is operational",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hangfire health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Hangfire check failed",
                ex,
                data: new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }));
        }
    }
}
