using Podium.Core.Interfaces; // Changed from Podium.Infrastructure.Data
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace Podium.API.Jobs
{
    public class CleanOldAuditLogsJob
    {
        private readonly IUnitOfWork _unitOfWork;

        public CleanOldAuditLogsJob(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task ExecuteAsync()
        {
            // Delete logs older than 1 year
            var cutoffDate = DateTime.UtcNow.AddYears(-1);

            // ExecuteDeleteAsync is efficient for bulk deletes in EF Core 7+
            // We access GetQueryable() to get the IQueryable interface that supports ExecuteDeleteAsync
            var deletedCount = await _unitOfWork.AuditLogs.GetQueryable()
                .Where(x => x.CreatedAt < cutoffDate)
                .ExecuteDeleteAsync();
        }
    }
}