using Podium.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Podium.API.Jobs
{
    public class CleanOldAuditLogsJob
    {
        private readonly ApplicationDbContext _context;

        public CleanOldAuditLogsJob(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ExecuteAsync()
        {
            // Delete logs older than 1 year
            var cutoffDate = DateTime.UtcNow.AddYears(-1);

            // ExecuteDeleteAsync is efficient for bulk deletes in EF Core 7+
            var deletedCount = await _context.AuditLogs
                .Where(x => x.Timestamp < cutoffDate)
                .ExecuteDeleteAsync();
        }
    }
}
