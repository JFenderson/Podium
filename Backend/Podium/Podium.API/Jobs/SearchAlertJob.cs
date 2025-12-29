using System;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Logging;
using Podium.Application.Interfaces;

namespace Podium.Infrastructure.BackgroundJobs
{
    public class SearchAlertJob
    {
        private readonly ISavedSearchService _savedSearchService;
        private readonly ILogger<SearchAlertJob> _logger;

        public SearchAlertJob(ISavedSearchService savedSearchService, ILogger<SearchAlertJob> logger)
        {
            _savedSearchService = savedSearchService;
            _logger = logger;
        }

        public async Task ProcessSearchAlerts()
        {
            try
            {
                _logger.LogInformation("Starting search alert processing at {Time}", DateTime.UtcNow);

                await _savedSearchService.ProcessSearchAlertsAsync();

                _logger.LogInformation("Completed search alert processing at {Time}", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing search alerts");
                throw;
            }
        }

        public static void Configure()
        {
            // Run every hour
            RecurringJob.AddOrUpdate<SearchAlertJob>(
                "process-search-alerts",
                job => job.ProcessSearchAlerts(),
                Cron.Hourly);
        }
    }
}

// Add this to your Startup.cs or Program.cs after Hangfire initialization:
// SearchAlertJob.Configure();