using Microsoft.Extensions.Logging;
using Podium.Core.Interfaces;

namespace Podium.Infrastructure.Services
{
    public class MockEmailService : IEmailService
    {
        private readonly ILogger<MockEmailService> _logger;

        public MockEmailService(ILogger<MockEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailAsync(string to, string subject, string body)
        {
            // Instead of sending, we just log it to the console/debug window
            _logger.LogInformation("--------------------------------------------------");
            _logger.LogInformation($"[Mock Email Sent]");
            _logger.LogInformation($"To: {to}");
            _logger.LogInformation($"Subject: {subject}");
            _logger.LogInformation($"Body: {body}");
            _logger.LogInformation("--------------------------------------------------");

            return Task.CompletedTask;
        }
    }
}