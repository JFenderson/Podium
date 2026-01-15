using Microsoft.Extensions.Logging;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Telemetry;

/// <summary>
/// No-op implementation of telemetry service used when Application Insights is disabled.
/// All methods log at debug level and perform no actual telemetry tracking.
/// </summary>
public class NoOpTelemetryService : ITelemetryService
{
    private readonly ILogger<NoOpTelemetryService> _logger;

    public NoOpTelemetryService(ILogger<NoOpTelemetryService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public void TrackAuthenticationAttempt(string userId, bool success, string reason)
    {
        _logger.LogDebug("Application Insights disabled - skipping authentication attempt telemetry for user {UserId}", userId);
    }

    /// <inheritdoc/>
    public void TrackVideoUpload(string userId, long fileSizeBytes, TimeSpan duration, bool success)
    {
        _logger.LogDebug("Application Insights disabled - skipping video upload telemetry for user {UserId}", userId);
    }

    /// <inheritdoc/>
    public void TrackDatabaseQuery(string queryName, TimeSpan duration)
    {
        _logger.LogDebug("Application Insights disabled - skipping database query telemetry for {QueryName}", queryName);
    }

    /// <inheritdoc/>
    public void TrackApiEndpoint(string endpoint, TimeSpan responseTime, int statusCode)
    {
        _logger.LogDebug("Application Insights disabled - skipping API endpoint telemetry for {Endpoint}", endpoint);
    }

    /// <inheritdoc/>
    public void TrackException(Exception ex, Dictionary<string, string> properties)
    {
        _logger.LogDebug("Application Insights disabled - skipping exception telemetry for {ExceptionType}", ex.GetType().Name);
    }
}
