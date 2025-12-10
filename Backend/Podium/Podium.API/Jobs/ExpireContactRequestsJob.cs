using Podium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Podium.API.Jobs
{
    public class ExpireContactRequestsJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpireContactRequestsJob> _logger;

        public ExpireContactRequestsJob(ApplicationDbContext context, ILogger<ExpireContactRequestsJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ExpireContactRequestsJob...");

            // Threshold: 30 days ago
            var expirationThreshold = DateTime.UtcNow.AddDays(-30);

            var expiredRequests = await _context.ContactRequests
                .Where(r => r.Status == "Pending" && r.RequestedDate < expirationThreshold)
                .ToListAsync();

            foreach (var request in expiredRequests)
            {
                request.Status = "Expired";
                request.DeclineReason = "Auto-expired after 30 days of inactivity.";
                request.ResponseDate = DateTime.UtcNow;
            }

            if (expiredRequests.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Expired {expiredRequests.Count} contact requests.");
            }
        }
    }
}
