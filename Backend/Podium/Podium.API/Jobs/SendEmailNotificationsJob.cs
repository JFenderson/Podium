using Hangfire;
using Podium.Core.Interfaces;

namespace Podium.API.Jobs
{
    public class SendEmailNotificationsJob
    {
        private readonly IEmailService _emailService;

        public SendEmailNotificationsJob(IEmailService emailService)
        {
            _emailService = emailService;
        }

        // Hangfire will automatically retry this job on failure based on default settings (usually 10 attempts)
        [AutomaticRetry(Attempts = 5)]
        public async Task ExecuteAsync(string to, string subject, string body)
        {
            await _emailService.SendEmailAsync(to, subject, body);
        }
    }
}
