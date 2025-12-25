using Podium.Core.Constants;

namespace Podium.Application.DTOs.Guardian
{
    public class NotificationFilterDto
    {
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public NotificationPriority? Priority { get; set; } // Added
        public DateTime? Since { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}