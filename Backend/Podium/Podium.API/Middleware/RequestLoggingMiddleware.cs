using Serilog.Context;
using System.Diagnostics;

namespace Podium.API.Middleware;

/// <summary>
/// Middleware for comprehensive request logging with correlation IDs and structured properties.
/// Logs request start, completion, duration, and sanitizes sensitive data.
/// </summary>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Generate or use existing correlation ID
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? context.TraceIdentifier;

        // Add correlation ID to response headers
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        // Push correlation ID to Serilog LogContext
        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", context.TraceIdentifier))
        {
            var stopwatch = Stopwatch.StartNew();
            var request = context.Request;

            // Get user ID if authenticated
            var userId = context.User?.Identity?.IsAuthenticated == true
                ? context.User.FindFirst("sub")?.Value 
                    ?? context.User.FindFirst("userId")?.Value
                    ?? context.User.Identity.Name
                : "Anonymous";

            using (LogContext.PushProperty("UserId", userId))
            {
                // Log request start
                _logger.LogInformation(
                    "HTTP {Method} {Path} started from {IpAddress}",
                    request.Method,
                    GetSanitizedPath(request.Path),
                    context.Connection.RemoteIpAddress?.ToString() ?? "Unknown");

                try
                {
                    // Call the next middleware in the pipeline
                    await _next(context);
                }
                finally
                {
                    stopwatch.Stop();

                    var statusCode = context.Response.StatusCode;
                    var duration = stopwatch.ElapsedMilliseconds;

                    // Determine log level based on status code and duration
                    var logLevel = GetLogLevel(statusCode, duration);

                    _logger.Log(
                        logLevel,
                        "HTTP {Method} {Path} responded {StatusCode} in {DurationMs}ms",
                        request.Method,
                        GetSanitizedPath(request.Path),
                        statusCode,
                        duration);
                }
            }
        }
    }

    /// <summary>
    /// Determines the appropriate log level based on status code and duration.
    /// </summary>
    private static LogLevel GetLogLevel(int statusCode, long durationMs)
    {
        // Error responses
        if (statusCode >= 500)
            return LogLevel.Error;

        if (statusCode >= 400)
            return LogLevel.Warning;

        // Slow requests (>2000ms)
        if (durationMs > 2000)
            return LogLevel.Warning;

        return LogLevel.Information;
    }

    /// <summary>
    /// Sanitizes the request path to avoid logging sensitive data in URLs.
    /// </summary>
    private static string GetSanitizedPath(PathString path)
    {
        var pathValue = path.Value ?? "/";

        // Sanitize common sensitive patterns in URLs
        if (pathValue.Contains("token=", StringComparison.OrdinalIgnoreCase))
        {
            pathValue = System.Text.RegularExpressions.Regex.Replace(
                pathValue,
                @"token=[^&]+",
                "token=***REDACTED***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        if (pathValue.Contains("password=", StringComparison.OrdinalIgnoreCase))
        {
            pathValue = System.Text.RegularExpressions.Regex.Replace(
                pathValue,
                @"password=[^&]+",
                "password=***REDACTED***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        if (pathValue.Contains("apikey=", StringComparison.OrdinalIgnoreCase))
        {
            pathValue = System.Text.RegularExpressions.Regex.Replace(
                pathValue,
                @"apikey=[^&]+",
                "apikey=***REDACTED***",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return pathValue;
    }
}
