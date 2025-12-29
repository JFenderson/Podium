namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Personal metrics for band staff member
    /// </summary>
    public class BandStaffPersonalMetricsDto
    {
        // Offers
        public int OffersCreated { get; set; }
        public int OffersAccepted { get; set; }
        public double AcceptanceRate { get; set; }
        public double AcceptanceRateChange { get; set; }

        // Students
        public int StudentsContacted { get; set; }
        public int StudentsResponded { get; set; }
        public double ResponseRate { get; set; }
        public double ResponseRateChange { get; set; }

        // Budget
        public decimal BudgetAllocated { get; set; }
        public decimal BudgetUsed { get; set; }
        public decimal BudgetRemaining { get; set; }
        public double BudgetUtilization { get; set; }

        // Activity
        public int DaysSinceLastActivity { get; set; }
        public int RatingsGiven { get; set; }
        public double AverageOfferAmount { get; set; }

        // Rankings
        public int? MyRankByOffers { get; set; }
        public int? MyRankByAcceptance { get; set; }
        public int TotalStaff { get; set; }
    }
}