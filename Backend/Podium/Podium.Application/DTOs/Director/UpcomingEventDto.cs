using Podium.Application.DTOs.Offer;

namespace Podium.Application.DTOs.Director
{
    public class UpcomingEventDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string EventType { get; set; } = string.Empty;
        public int RegisteredCount { get; set; }
        public int CapacityLimit { get; set; }
        public bool IsRegistrationOpen { get; set; }
    }
}