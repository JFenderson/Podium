using Podium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Hangfire;

namespace Podium.API.Jobs
{
    public class ArchiveInactiveStudentsJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<ArchiveInactiveStudentsJob> _logger;

        public ArchiveInactiveStudentsJob(ApplicationDbContext context, IBackgroundJobClient backgroundJobClient, ILogger<ArchiveInactiveStudentsJob> logger)
        {
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ArchiveInactiveStudentsJob...");

            var archiveThreshold = DateTime.UtcNow.AddMonths(-24);
            var warningThreshold = DateTime.UtcNow.AddMonths(-23); // Send warning 1 month before

            // 1. Handle Archiving (Assuming an IsActive flag exists on User or similar logic)
            // Since Student entity provided doesn't have IsActive, we check LastActivityDate
            var studentsToArchive = await _context.Students
                .Include(s => s.ApplicationUser)
                .Where(s => s.LastActivityDate < archiveThreshold) // && s.ApplicationUser.IsActive
                .ToListAsync();

            foreach (var student in studentsToArchive)
            {
                // Logic to archive: e.g., set flag, move data, or lock account
                // student.ApplicationUser.IsActive = false; // Example
                _logger.LogInformation($"Archiving student {student.Id} due to inactivity.");

                // Send final notification
                _backgroundJobClient.Enqueue<SendEmailNotificationsJob>(job =>
                   job.ExecuteAsync(student.Email, "Account Archived", "Your account has been archived due to inactivity."));
            }

            // 2. Handle Warnings
            var studentsToWarn = await _context.Students
                .Where(s => s.LastActivityDate < warningThreshold && s.LastActivityDate > archiveThreshold)
                .ToListAsync();

            foreach (var student in studentsToWarn)
            {
                _backgroundJobClient.Enqueue<SendEmailNotificationsJob>(job =>
                   job.ExecuteAsync(student.Email, "Action Required: Account Expiration", "Please log in to keep your account active."));
            }

            await _context.SaveChangesAsync();
        }
    }
}
