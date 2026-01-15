namespace Podium.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Prevents MIME type sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Prevents clickjacking attacks
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Enables XSS protection in browsers
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Strict-Transport-Security (HSTS): Forces HTTPS
        var enableHSTS = _configuration.GetValue<bool>("SecurityHeaders:EnableHSTS", false);
        if (enableHSTS)
        {
            var maxAge = _configuration.GetValue<int>("SecurityHeaders:HSTSMaxAge", 31536000);
            context.Response.Headers.Append("Strict-Transport-Security", $"max-age={maxAge}; includeSubDomains");
        }

        // Content-Security-Policy: Prevents various injection attacks
        var enableCSP = _configuration.GetValue<bool>("SecurityHeaders:EnableCSP", true);
        if (enableCSP)
        {
            // Relaxed CSP for development - should be stricter in production
            context.Response.Headers.Append("Content-Security-Policy",
                "default-src 'self'; " +
                "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                "style-src 'self' 'unsafe-inline'; " +
                "img-src 'self' data: https:; " +
                "font-src 'self' data:; " +
                "connect-src 'self' ws: wss:; " +
                "frame-ancestors 'none';");
        }

        // Referrer-Policy: Controls referrer information
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Permissions-Policy: Controls browser features
        context.Response.Headers.Append("Permissions-Policy",
            "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");

        await _next(context);
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
