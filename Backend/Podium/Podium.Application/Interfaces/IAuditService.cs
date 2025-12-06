using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(string userId, string actionType, string description, object? metadata = null);
        Task LogUnauthorizedAccessAsync(string userId, string resourceType, int resourceId);
        Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filters);
    }
}
