using System.Net;
using System.Text.Json;

namespace Podium.API.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continue down the pipeline
                await _next(context);
            }
            catch (Exception ex)
            {
                // Catch any exception that bubbles up
                _logger.LogError(ex, "An unhandled exception occurred processing the request.");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            int statusCode;
            string message;

            // Determine Status Code based on Exception Type
            switch (exception)
            {
                case KeyNotFoundException:
                    statusCode = (int)HttpStatusCode.NotFound;
                    message = "Resource Not Found";
                    break;

                // You can add more specific cases here (e.g., UnauthorizedAccessException -> 401)

                default:
                    statusCode = (int)HttpStatusCode.InternalServerError;
                    message = "Internal Server Error";
                    break;
            }

            context.Response.StatusCode = statusCode;

            // Construct JSON Response
            var response = new
            {
                message = message,
                detail = exception.Message
            };

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}