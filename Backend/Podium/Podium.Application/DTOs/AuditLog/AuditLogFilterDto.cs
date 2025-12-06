using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.AuditLog
{
    public class AuditLogFilterDto
    {
        public string? UserId { get; set; }
        public string? ActionType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool SecurityEventsOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
