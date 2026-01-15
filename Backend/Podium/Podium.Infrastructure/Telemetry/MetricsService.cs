using Microsoft.ApplicationInsights;

namespace Podium.Infrastructure.Telemetry;

/// <summary>
/// Service for tracking custom metrics in Application Insights.
/// Provides methods to track business-specific metrics like active users,
/// video processing time, and conversion rates.
/// </summary>
public class MetricsService
{
    private readonly TelemetryClient _telemetryClient;

    public MetricsService(TelemetryClient telemetryClient)
    {
        _telemetryClient = telemetryClient;
    }

    /// <summary>
    /// Tracks the current number of active users.
    /// </summary>
    /// <param name="count">Number of active users</param>
    public void TrackActiveUsers(int count)
    {
        _telemetryClient.GetMetric("ActiveUsers").TrackValue(count);
    }

    /// <summary>
    /// Tracks video processing time in milliseconds.
    /// </summary>
    /// <param name="durationMs">Processing duration in milliseconds</param>
    public void TrackVideoProcessingTime(double durationMs)
    {
        _telemetryClient.GetMetric("VideoProcessingTime").TrackValue(durationMs);
    }

    /// <summary>
    /// Tracks scholarship offer conversion events.
    /// </summary>
    /// <param name="wasAccepted">Whether the offer was accepted (true) or rejected (false)</param>
    public void TrackScholarshipOfferConversion(bool wasAccepted)
    {
        var metricName = "ScholarshipOfferConversion";
        _telemetryClient.GetMetric(metricName, "Status")
            .TrackValue(wasAccepted ? 1 : 0, wasAccepted ? "Accepted" : "Rejected");
    }

    /// <summary>
    /// Tracks API success/failure rates.
    /// </summary>
    /// <param name="endpoint">The API endpoint</param>
    /// <param name="success">Whether the API call was successful</param>
    public void TrackApiSuccessRate(string endpoint, bool success)
    {
        _telemetryClient.GetMetric("ApiSuccessRate", "Endpoint", "Status")
            .TrackValue(1, endpoint, success ? "Success" : "Failure");
    }

    /// <summary>
    /// Tracks custom event with a numeric value.
    /// </summary>
    /// <param name="metricName">Name of the metric</param>
    /// <param name="value">Metric value</param>
    /// <param name="properties">Optional properties for filtering</param>
    public void TrackCustomMetric(string metricName, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient.TrackMetric(metricName, value, properties);
    }
}
