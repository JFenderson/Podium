using Podium.Core.Interfaces; // Changed from Podium.Infrastructure.Data
using Podium.Core.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace Podium.API.Jobs
{
    public class ExpireScholarshipOffersJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExpireScholarshipOffersJob> _logger;

        public ExpireScholarshipOffersJob(IUnitOfWork unitOfWork, ILogger<ExpireScholarshipOffersJob> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ExpireScholarshipOffersJob...");

            var now = DateTime.UtcNow;

            // Find offers that are still pending/draft but past their expiration date
            var expiredOffers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => (o.Status == ScholarshipStatus.Pending || o.Status == ScholarshipStatus.Draft)
                            && o.ExpirationDate < now)
                .ToListAsync();

            foreach (var offer in expiredOffers)
            {
                offer.Status = ScholarshipStatus.Expired;
                offer.ResponseNotes = "Auto-expired by system.";
                // UnitOfWork pattern typically requires explicit Update call if not tracking by default, 
                // though usually EF Core tracks fetched entities. 
                // Adding Update for clarity/safety depending on repo implementation.
                _unitOfWork.ScholarshipOffers.Update(offer);
            }

            if (expiredOffers.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Expired {expiredOffers.Count} scholarship offers.");
            }
        }
    }
}