namespace Podium.Application.DTOs.Band
{
    public class ScholarshipTrendDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int OffersCreated { get; set; }
        public int OffersAccepted { get; set; }
        public decimal TotalAmount { get; set; }
    }
}