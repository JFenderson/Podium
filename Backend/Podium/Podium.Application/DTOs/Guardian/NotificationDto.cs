using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Notification for guardian with detailed information.
    /// </summary>
    public class NotificationDto
    {
        public int NotificationId { get; set; }
        public string Type { get; set; } = string.Empty; // "NewOffer", "ContactRequest", "OfferExpiring", "StudentActivity"
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsRead { get; set; }
        public bool IsUrgent { get; set; }
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? ActionUrl { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
    }




}
