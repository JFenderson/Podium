namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Activity item for this staff member
    /// </summary>
    public class MyActivityDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; } = string.Empty; // OfferCreated, ContactMade, RatingGiven, OfferAccepted
        public string Description { get; set; } = string.Empty;
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? Details { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }
}