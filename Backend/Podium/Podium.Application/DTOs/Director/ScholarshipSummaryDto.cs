using Podium.Application.DTOs.Offer;

namespace Podium.Application.DTOs.Director
{
    public class ScholarshipSummaryDto
    {
        public int TotalOffersMade { get; set; }
        public int PendingOffers { get; set; }
        public int AcceptedOffers { get; set; }
        public int DeclinedOffers { get; set; }
        public decimal TotalCommittedAmount { get; set; }
        public decimal AvailableBudget { get; set; }
        public decimal BudgetUtilizationPercentage { get; set; }
    }
}