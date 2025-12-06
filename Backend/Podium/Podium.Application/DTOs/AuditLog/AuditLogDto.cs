using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.AuditLog
{
    public class AuditLogDto
    {
        public int VideoId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty; // Alias for ApplicationUserId
        public string ActionType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public bool IsSecurityEvent { get; set; }
        public string? Severity { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
