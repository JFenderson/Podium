namespace Podium.Application.DTOs.Offer
{
    public class ScholarshipBudgetDto
    {
        public decimal TotalBudget { get; set; }
        public decimal AllocatedAmount { get; set; }
        public decimal CommittedAmount { get; set; } // Accepted offers
        public decimal AvailableAmount { get; set; }
        public decimal PendingAmount { get; set; } // Pending + Approved but not responded
    }
}