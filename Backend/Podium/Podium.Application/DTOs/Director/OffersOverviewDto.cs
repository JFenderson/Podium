namespace Podium.Application.DTOs.Director
{
    public class OffersOverviewDto
    {
        // Time series data
        public List<OfferTimeSeriesDto> OffersByMonth { get; set; }

        // Breakdowns
        public List<OfferBreakdownDto> OffersByInstrument { get; set; }
        public List<OfferBreakdownDto> OffersByStatus { get; set; }
        public List<OfferBreakdownDto> OffersByRecruiter { get; set; }

        // Summary
        public int TotalOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int PendingOffers { get; set; }
        public int DeclinedOffers { get; set; }
        public int ExpiredOffers { get; set; }
    }
}