namespace Podium.Application.DTOs.Director
{
    public class ActivityItemDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } // OfferSent, OfferAccepted, OfferDeclined, ContactMade, VideoUploaded, InterestShown, StaffAction

        // Primary actor
        public string ActorType { get; set; } // Student, Staff, System
        public int? ActorId { get; set; }
        public string? ActorName { get; set; }

        // Related entities
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }

        public int? StaffId { get; set; }
        public string? StaffName { get; set; }

        public int? OfferId { get; set; }

        // Activity details
        public string Description { get; set; }
        public string? Details { get; set; }
        public object? Metadata { get; set; }
    }
}