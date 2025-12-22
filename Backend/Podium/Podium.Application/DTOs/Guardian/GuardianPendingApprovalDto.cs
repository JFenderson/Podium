namespace Podium.Application.DTOs.Guardian
{
    public class GuardianPendingApprovalDto
    {
        public int OfferId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OfferType { get; set; } = string.Empty; // e.g. "Full Tuition"
        public DateTime DateReceived { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}