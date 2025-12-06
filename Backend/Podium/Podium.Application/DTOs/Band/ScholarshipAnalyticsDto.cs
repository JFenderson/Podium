namespace Podium.Application.DTOs.Band
{
    public class ScholarshipAnalyticsDto
    {
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal AcceptedAmount { get; set; }
        public decimal AvailableAmount { get; set; }
        public decimal AverageOfferAmount { get; set; }
        public decimal MedianOfferAmount { get; set; }
        public int TotalOffers { get; set; }
        public double AcceptanceRate { get; set; } // Percentage of offers accepted
        public List<ScholarshipTrendDto> MonthlyTrends { get; set; } = new();
    }
}