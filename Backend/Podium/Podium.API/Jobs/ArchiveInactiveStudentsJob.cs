using Podium.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace Podium.API.Jobs
{
    public class ArchiveInactiveStudentsJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<ArchiveInactiveStudentsJob> _logger;

        public ArchiveInactiveStudentsJob(
            IUnitOfWork unitOfWork,
            IBackgroundJobClient backgroundJobClient,
            ILogger<ArchiveInactiveStudentsJob> logger)
        {
            _unitOfWork = unitOfWork;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting ArchiveInactiveStudentsJob...");

            var archiveThreshold = DateTime.UtcNow.AddMonths(-24);
            var warningThreshold = DateTime.UtcNow.AddMonths(-23);

            // 1. Handle Archiving
            var studentsToArchive = await _unitOfWork.Students.GetQueryable()
                .Include(s => s.ApplicationUser)
                .Where(s => s.LastActivityDate < archiveThreshold)
                .ToListAsync();

            foreach (var student in studentsToArchive)
            {
                _logger.LogInformation($"Archiving student {student.Id} due to inactivity.");
                _backgroundJobClient.Enqueue<SendEmailNotificationsJob>(job =>
                   job.ExecuteAsync(student.Email, "Account Archived", "Your account has been archived due to inactivity."));

                // Logic to archive would go here (e.g. _unitOfWork.Students.Update(student))
            }

            // 2. Handle Warnings
            var studentsToWarn = await _unitOfWork.Students.GetQueryable()
                .Where(s => s.LastActivityDate < warningThreshold && s.LastActivityDate > archiveThreshold)
                .ToListAsync();

            foreach (var student in studentsToWarn)
            {
                _backgroundJobClient.Enqueue<SendEmailNotificationsJob>(job =>
                   job.ExecuteAsync(student.Email, "Action Required: Account Expiration", "Please log in to keep your account active."));
            }

            // Save any changes if students were modified
            if (studentsToArchive.Any())
            {
                await _unitOfWork.SaveChangesAsync();
            }
        }
    }
}