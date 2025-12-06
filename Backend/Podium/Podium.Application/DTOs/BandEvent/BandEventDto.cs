using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandEvent
{
    /// <summary>
    /// Band event with registration and attendance details.
    /// </summary>
    public class BandEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // Showcase, Camp, Audition, Info Session
        public DateTime EventDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Location { get; set; } = string.Empty;
        public int? CapacityLimit { get; set; }
        public int RegisteredCount { get; set; }
        public int AttendedCount { get; set; }
        public bool IsRegistrationOpen { get; set; }
        public DateTime? RegistrationDeadline { get; set; }
        public bool IsVirtual { get; set; }
        public string? MeetingLink { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
