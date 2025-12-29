namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Performance metrics for this staff member
    /// </summary>
    public class BandStaffPerformanceDto
    {
        // Funnel
        public int StudentsContacted { get; set; }
        public int StudentsInterested { get; set; }
        public int OffersExtended { get; set; }
        public int OffersAccepted { get; set; }
        public int StudentsEnrolled { get; set; }

        // Conversion rates
        public double ContactToInterestRate { get; set; }
        public double InterestToOfferRate { get; set; }
        public double OfferToAcceptanceRate { get; set; }
        public double OverallConversionRate { get; set; }

        // Time series
        public List<PerformanceTimeSeriesDto> MonthlyMetrics { get; set; } = new();

        // Comparisons
        public double MyAcceptanceRate { get; set; }
        public double TeamAverageAcceptanceRate { get; set; }
        public double MyResponseRate { get; set; }
        public double TeamAverageResponseRate { get; set; }
    }
}