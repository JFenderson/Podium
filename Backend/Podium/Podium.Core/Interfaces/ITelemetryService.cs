namespace Podium.Core.Interfaces;

/// <summary>
/// Service for tracking custom telemetry and metrics in Application Insights.
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Tracks an authentication attempt with success/failure status.
    /// </summary>
    /// <param name="userId">The user ID attempting authentication</param>
    /// <param name="success">Whether the authentication was successful</param>
    /// <param name="reason">The reason for failure (if applicable)</param>
    void TrackAuthenticationAttempt(string userId, bool success, string reason);

    /// <summary>
    /// Tracks a video upload operation with file size and duration metrics.
    /// </summary>
    /// <param name="userId">The user ID uploading the video</param>
    /// <param name="fileSizeBytes">The size of the uploaded file in bytes</param>
    /// <param name="duration">The duration of the upload operation</param>
    /// <param name="success">Whether the upload was successful</param>
    void TrackVideoUpload(string userId, long fileSizeBytes, TimeSpan duration, bool success);

    /// <summary>
    /// Tracks a database query execution with its duration.
    /// </summary>
    /// <param name="queryName">The name or identifier of the query</param>
    /// <param name="duration">The duration of the query execution</param>
    void TrackDatabaseQuery(string queryName, TimeSpan duration);

    /// <summary>
    /// Tracks an API endpoint call with response time and status code.
    /// </summary>
    /// <param name="endpoint">The API endpoint path</param>
    /// <param name="responseTime">The response time of the request</param>
    /// <param name="statusCode">The HTTP status code returned</param>
    void TrackApiEndpoint(string endpoint, TimeSpan responseTime, int statusCode);

    /// <summary>
    /// Tracks an exception with custom properties for better diagnostics.
    /// </summary>
    /// <param name="ex">The exception to track</param>
    /// <param name="properties">Additional properties for context</param>
    void TrackException(Exception ex, Dictionary<string, string> properties);
}
