namespace Podium.Application.DTOs.Band
{
    public class ConversionMetricsDto
    {
        // Funnel metrics
        public int StudentsInterested { get; set; }
        public int StudentsContacted { get; set; }
        public int StudentsOffered { get; set; }
        public int StudentsAccepted { get; set; }

        // Conversion rates
        public double InterestToContactRate { get; set; }
        public double ContactToOfferRate { get; set; }
        public double OfferToAcceptanceRate { get; set; }
        public double OverallConversionRate { get; set; }

        // Time metrics (in days)
        public double AverageTimeToContact { get; set; }
        public double AverageTimeToOffer { get; set; }
        public double AverageTimeToAcceptance { get; set; }
    }
}