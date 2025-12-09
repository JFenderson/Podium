namespace Podium.Core.Entities
{
    public class OfferHistory
    {
        public int Id { get; set; }
        public int ScholarshipOfferId { get; set; }
        public ScholarshipStatus OldStatus { get; set; }
        public ScholarshipStatus NewStatus { get; set; }
        public Guid ChangedById { get; set; } // Who did it?
        public string Note { get; set; } // e.g., Rescind reason
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}