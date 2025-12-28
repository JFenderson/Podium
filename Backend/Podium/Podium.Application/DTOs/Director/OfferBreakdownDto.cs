namespace Podium.Application.DTOs.Director
{
    public class OfferBreakdownDto
    {
        public string Label { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? AverageAmount { get; set; }
    }
}