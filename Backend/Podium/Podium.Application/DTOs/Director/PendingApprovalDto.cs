namespace Podium.Application.DTOs.Director
{
    public class PendingApprovalDto
    {
        public int ApprovalId { get; set; }
        public string Type { get; set; } // ScholarshipOffer, BudgetIncrease, StaffPermission

        // Scholarship specific
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? Instrument { get; set; }

        // Offer details
        public decimal? Amount { get; set; }
        public string? OfferType { get; set; }
        public string? Description { get; set; }

        // Staff details
        public int RequestedByStaffId { get; set; }
        public string RequestedByStaffName { get; set; }

        // Approval details
        public DateTime RequestDate { get; set; }
        public string Urgency { get; set; } // Low, Medium, High
        public string? Reason { get; set; }

        // Actions
        public bool CanApprove { get; set; }
        public bool CanDeny { get; set; }
    }
}