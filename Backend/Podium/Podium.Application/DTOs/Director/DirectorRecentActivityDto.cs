using Podium.Application.DTOs.Offer;

namespace Podium.Application.DTOs.Director
{
    public class DirectorRecentActivityDto
    {
        public string ActivityType { get; set; } = string.Empty; // "StudentInterest", "VideoUpload", "OfferAccepted", etc.
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? StudentName { get; set; }
        public string? StaffName { get; set; }
    }
}