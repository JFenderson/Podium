using Podium.Application.DTOs;
using Podium.Application.DTOs.AuditLog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IAuditService
    {
        Task LogActionAsync(string userId, string actionType, string description, object? metadata = null);
        Task LogSecurityEventAsync(string userId, string actionType, string description, string severity = "Medium", object? metadata = null);
        Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogFilterDto filters);
        Task LogUnauthorizedAccessAsync(string userId, string v, int studentId);
    }
}