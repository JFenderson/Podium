using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Telemetry;

/// <summary>
/// Implementation of telemetry tracking service using Application Insights.
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TelemetryService(TelemetryClient telemetryClient, IHttpContextAccessor httpContextAccessor)
    {
        _telemetryClient = telemetryClient;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc/>
    public void TrackAuthenticationAttempt(string userId, bool success, string reason)
    {
        var properties = new Dictionary<string, string>
        {
            { "UserId", userId },
            { "Success", success.ToString() },
            { "Reason", reason },
            { "Action", "Authentication" }
        };

        AddRequestContext(properties);

        var eventName = success ? "AuthenticationSuccess" : "AuthenticationFailure";
        _telemetryClient.TrackEvent(eventName, properties);

        // Track as metric for dashboards
        _telemetryClient.GetMetric("AuthenticationAttempts", "Success")
            .TrackValue(success ? 1 : 0);
    }

    /// <inheritdoc/>
    public void TrackVideoUpload(string userId, long fileSizeBytes, TimeSpan duration, bool success)
    {
        var properties = new Dictionary<string, string>
        {
            { "UserId", userId },
            { "FileSizeBytes", fileSizeBytes.ToString() },
            { "DurationMs", duration.TotalMilliseconds.ToString("F2") },
            { "Success", success.ToString() },
            { "Action", "VideoUpload" }
        };

        AddRequestContext(properties);

        var eventName = success ? "VideoUploadSuccess" : "VideoUploadFailure";
        _telemetryClient.TrackEvent(eventName, properties);

        // Track metrics
        _telemetryClient.GetMetric("VideoUploadSize").TrackValue(fileSizeBytes);
        _telemetryClient.GetMetric("VideoUploadDuration").TrackValue(duration.TotalMilliseconds);
        _telemetryClient.GetMetric("VideoUploadSuccessRate", "Success")
            .TrackValue(success ? 1 : 0);
    }

    /// <inheritdoc/>
    public void TrackDatabaseQuery(string queryName, TimeSpan duration)
    {
        var properties = new Dictionary<string, string>
        {
            { "QueryName", queryName },
            { "DurationMs", duration.TotalMilliseconds.ToString("F2") },
            { "Action", "DatabaseQuery" }
        };

        AddRequestContext(properties);

        _telemetryClient.TrackEvent("DatabaseQuery", properties);

        // Track as dependency for Application Insights
        var dependency = new DependencyTelemetry
        {
            Name = queryName,
            Type = "SQL",
            Duration = duration,
            Success = true
        };
        _telemetryClient.TrackDependency(dependency);

        // Track slow queries as separate metric
        if (duration.TotalMilliseconds > 500)
        {
            _telemetryClient.GetMetric("SlowQueries").TrackValue(1);
        }
    }

    /// <inheritdoc/>
    public void TrackApiEndpoint(string endpoint, TimeSpan responseTime, int statusCode)
    {
        var properties = new Dictionary<string, string>
        {
            { "Endpoint", endpoint },
            { "ResponseTimeMs", responseTime.TotalMilliseconds.ToString("F2") },
            { "StatusCode", statusCode.ToString() },
            { "Action", "ApiEndpoint" }
        };

        AddRequestContext(properties);

        _telemetryClient.TrackEvent("ApiEndpointCall", properties);

        // Track metrics
        _telemetryClient.GetMetric("ApiResponseTime", "Endpoint")
            .TrackValue(responseTime.TotalMilliseconds, endpoint);
        
        var isSuccess = statusCode >= 200 && statusCode < 300;
        _telemetryClient.GetMetric("ApiSuccessRate", "Endpoint")
            .TrackValue(isSuccess ? 1 : 0, endpoint);
    }

    /// <inheritdoc/>
    public void TrackException(Exception ex, Dictionary<string, string> properties)
    {
        AddRequestContext(properties);
        properties["Action"] = "Exception";

        _telemetryClient.TrackException(ex, properties);
    }

    /// <summary>
    /// Adds request context information (RequestId, UserId, IP) to telemetry properties.
    /// </summary>
    private void AddRequestContext(Dictionary<string, string> properties)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Add correlation ID
            if (httpContext.TraceIdentifier != null)
            {
                properties["RequestId"] = httpContext.TraceIdentifier;
            }

            // Add user ID if authenticated
            if (httpContext.User?.Identity?.IsAuthenticated == true)
            {
                var userId = httpContext.User.FindFirst("sub")?.Value 
                    ?? httpContext.User.FindFirst("userId")?.Value
                    ?? httpContext.User.Identity.Name;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    properties["AuthenticatedUserId"] = userId;
                }
            }

            // Add IP address
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                properties["IpAddress"] = ipAddress;
            }
        }
    }
}
