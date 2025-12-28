namespace Podium.Application.DTOs.Director
{
    public class StaffPerformanceDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }

        // Offer Metrics
        public int OffersCreated { get; set; }
        public int OffersAccepted { get; set; }
        public double AcceptanceRate { get; set; }

        // Contact Metrics
        public int StudentsContacted { get; set; }
        public int StudentsResponded { get; set; }
        public double ResponseRate { get; set; }

        // Budget
        public decimal TotalBudgetAllocated { get; set; }
        public decimal AverageOfferAmount { get; set; }

        // Activity
        public DateTime LastActivityDate { get; set; }
        public int DaysActive { get; set; }

        // Rankings
        public int? PerformanceRank { get; set; }
        public int? AcceptanceRateRank { get; set; }
    }
}