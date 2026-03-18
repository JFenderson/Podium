using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Podium.Core.Interfaces;
using Polly;
using Polly.Retry;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Podium.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        private static readonly ResiliencePipeline RetryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromSeconds(2),
                BackoffType = DelayBackoffType.Exponential,
                ShouldHandle = new PredicateBuilder().Handle<Exception>()
            })
            .Build();

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("SendGrid API key is not configured.");

            await RetryPipeline.ExecuteAsync(async cancellationToken =>
            {
                var client = new SendGridClient(apiKey);
                var from = new EmailAddress(
                    _configuration["SendGrid:FromEmail"] ?? "noreply@podiumapp.com",
                    _configuration["SendGrid:FromName"] ?? "Podium Team");
                var toAddress = new EmailAddress(to);
                var msg = MailHelper.CreateSingleEmail(from, toAddress, subject, body, body);

                var response = await client.SendEmailAsync(msg, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SendGrid returned {StatusCode} for email to {Recipient}. Will retry.", response.StatusCode, to);
                    throw new Exception($"Failed to send email. Status: {response.StatusCode}");
                }

                _logger.LogInformation("Email sent successfully to {Recipient}", to);
            });
        }
    }
}
