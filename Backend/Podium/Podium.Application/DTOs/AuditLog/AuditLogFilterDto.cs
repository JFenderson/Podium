using System;

namespace Podium.Application.DTOs.AuditLog
{
    public class AuditLogFilterDto
    {
        public string? ApplicationUserId { get; set; }
        public string? UserId { get; set; } // Alias for ApplicationUserId for backwards compatibility
        public string? ActionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool SecurityEventsOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
