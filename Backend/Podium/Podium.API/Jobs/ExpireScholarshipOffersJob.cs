using Podium.Infrastructure.Data;
using Podium.Core.Constants;
using Microsoft.EntityFrameworkCore;

namespace Podium.API.Jobs
{
    public class ExpireScholarshipOffersJob
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExpireScholarshipOffersJob> _logger;

        public ExpireScholarshipOffersJob(ApplicationDbContext context, ILogger<ExpireScholarshipOffersJob> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ExpireScholarshipOffersJob...");

            var now = DateTime.UtcNow;

            // Find offers that are still pending/draft but past their expiration date
            var expiredOffers = await _context.Offers
                .Where(o => (o.Status == ScholarshipStatus.Pending || o.Status == ScholarshipStatus.Draft)
                            && o.ExpirationDate < now)
                .ToListAsync();

            foreach (var offer in expiredOffers)
            {
                offer.Status = ScholarshipStatus.Expired;
                offer.ResponseNotes = "Auto-expired by system.";
            }

            if (expiredOffers.Any())
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Expired {expiredOffers.Count} scholarship offers.");
            }
        }
    }
}
