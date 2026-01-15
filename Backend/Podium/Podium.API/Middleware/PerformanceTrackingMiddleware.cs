using Microsoft.ApplicationInsights;
using System.Diagnostics;

namespace Podium.API.Middleware;

/// <summary>
/// Middleware for tracking API endpoint performance and sending metrics to Application Insights.
/// Logs slow requests (>2000ms) as warnings.
/// </summary>
public class PerformanceTrackingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<PerformanceTrackingMiddleware> _logger;
    private readonly TelemetryClient? _telemetryClient;

    public PerformanceTrackingMiddleware(
        RequestDelegate next,
        ILogger<PerformanceTrackingMiddleware> logger,
        TelemetryClient telemetryClient)
    {
        _next = next;
        _logger = logger;
        _telemetryClient = telemetryClient;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = context.Request;
        var endpoint = $"{request.Method} {request.Path}";

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var duration = stopwatch.Elapsed;
            var statusCode = context.Response.StatusCode;

            // Track performance metric in Application Insights
            var properties = new Dictionary<string, string>
            {
                { "Endpoint", endpoint },
                { "Method", request.Method },
                { "Path", request.Path },
                { "StatusCode", statusCode.ToString() },
                { "DurationMs", duration.TotalMilliseconds.ToString("F2") }
            };

            // Add user information if available
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                var userId = context.User.FindFirst("sub")?.Value 
                    ?? context.User.FindFirst("userId")?.Value
                    ?? context.User.Identity.Name;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    properties["UserId"] = userId;
                }
            }

            // Track the request in Application Insights
            _telemetryClient.TrackEvent("ApiRequest", properties);

            // Log warning for slow requests (>2000ms)
            if (duration.TotalMilliseconds > 2000)
            {
                _logger.LogWarning(
                    "Slow request detected: {Endpoint} took {DurationMs}ms with status {StatusCode}",
                    endpoint,
                    duration.TotalMilliseconds,
                    statusCode);

                // Track slow request as a separate event
                _telemetryClient.TrackEvent("SlowRequest", properties);
            }

            // Track response time metric
            _telemetryClient.GetMetric("RequestDuration", "Endpoint")
                .TrackValue(duration.TotalMilliseconds, endpoint);
        }
    }
}
