namespace Podium.Application.DTOs.Director
{
    public class OfferTimeSeriesDto
    {
        public DateTime Date { get; set; }
        public string Month { get; set; }
        public int TotalOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int DeclinedOffers { get; set; }
        public decimal AverageAmount { get; set; }
    }
}