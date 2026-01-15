using Microsoft.AspNetCore.Mvc;
using Podium.Core.Constants;

namespace Podium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ErrorLogsController : ControllerBase
{
    private readonly ILogger<ErrorLogsController> _logger;

    public ErrorLogsController(ILogger<ErrorLogsController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Receives error logs from the frontend application.
    /// </summary>
    /// <param name="errorDto">The error information from the client</param>
    /// <returns>Acknowledgment of receipt</returns>
    [HttpPost]
    public IActionResult LogFrontendError([FromBody] ClientErrorDto errorDto)
    {
        if (errorDto == null)
        {
            return BadRequest("Error data is required");
        }

        // Sanitize data before logging
        var sanitizedUrl = SanitizeUrl(errorDto.Url);
        var sanitizedMessage = SanitizeMessage(errorDto.Message);

        // Log the error with structured properties
        _logger.LogError(
            "Frontend error: {Message} at {Url} | User: {UserId} | Browser: {UserAgent} | Stack: {Stack}",
            sanitizedMessage,
            sanitizedUrl,
            errorDto.UserId ?? "Anonymous",
            SanitizeBrowserInfo(errorDto.UserAgent),
            errorDto.Stack);

        // Add breadcrumbs if available
        if (errorDto.Breadcrumbs != null && errorDto.Breadcrumbs.Any())
        {
            _logger.LogInformation(
                "Error breadcrumbs: {Breadcrumbs}",
                string.Join(" -> ", errorDto.Breadcrumbs));
        }

        return Ok(new { message = "Error logged successfully", timestamp = DateTime.UtcNow });
    }

    private static string SanitizeUrl(string? url)
    {
        if (string.IsNullOrEmpty(url))
            return "Unknown";

        try
        {
            // Remove query parameters that might contain sensitive data
            var uri = new Uri(url, UriKind.RelativeOrAbsolute);
            return uri.IsAbsoluteUri ? $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}" : url;
        }
        catch (UriFormatException)
        {
            // If URL is malformed, return sanitized version
            return url.Split('?')[0]; // Remove query string
        }
    }

    private static string SanitizeMessage(string? message)
    {
        if (string.IsNullOrEmpty(message))
            return "No message provided";

        // Truncate very long messages
        return message.Length > 1000 ? message.Substring(0, 1000) + "..." : message;
    }

    private static string SanitizeBrowserInfo(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return "Unknown";

        // Truncate user agent string
        return userAgent.Length > 200 ? userAgent.Substring(0, 200) + "..." : userAgent;
    }
}

/// <summary>
/// DTO for receiving client-side error information.
/// </summary>
public class ClientErrorDto
{
    /// <summary>
    /// Error message from the client
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Stack trace (if available)
    /// </summary>
    public string? Stack { get; set; }

    /// <summary>
    /// URL where the error occurred
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Timestamp when the error occurred (ISO 8601)
    /// </summary>
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// Browser user agent string
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// User ID if authenticated
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Breadcrumbs showing user actions leading to the error
    /// </summary>
    public List<string>? Breadcrumbs { get; set; }
}
