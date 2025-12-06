using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Offer
{
    /// <summary>
    /// Scholarship offer with comprehensive details for director view.
    /// </summary>
    public class ScholarshipOfferDto
    {
        public int OfferId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string OfferType { get; set; } = string.Empty;
        public decimal? ScholarshipAmount { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Notes { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public string? ApprovedByName { get; set; }
        public bool RequiresGuardianApproval { get; set; }
        public string? RescindReason { get; set; }
    }
}
