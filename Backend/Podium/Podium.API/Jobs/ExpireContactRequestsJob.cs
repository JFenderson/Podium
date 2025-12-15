using Podium.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Podium.API.Jobs
{
    public class ExpireContactRequestsJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<ExpireContactRequestsJob> _logger;

        public ExpireContactRequestsJob(IUnitOfWork unitOfWork, ILogger<ExpireContactRequestsJob> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ExpireContactRequestsJob...");

            var expirationThreshold = DateTime.UtcNow.AddDays(-30);

            var expiredRequests = await _unitOfWork.ContactRequests.GetQueryable()
                .Where(r => r.Status == "Pending" && r.RequestedDate < expirationThreshold)
                .ToListAsync();

            foreach (var request in expiredRequests)
            {
                request.Status = "Expired";
                request.DeclineReason = "Auto-expired after 30 days of inactivity.";
                request.ResponseDate = DateTime.UtcNow;
                _unitOfWork.ContactRequests.Update(request);
            }

            if (expiredRequests.Any())
            {
                await _unitOfWork.SaveChangesAsync();
                _logger.LogInformation($"Expired {expiredRequests.Count} contact requests.");
            }
        }
    }
}