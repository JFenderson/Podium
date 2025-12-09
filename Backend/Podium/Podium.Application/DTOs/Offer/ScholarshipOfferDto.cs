using Podium.Core.Entities;
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
        public decimal? ScholarshipAmount { get; set; }
        public ScholarshipStatus Status { get; set; }
        public string OfferType { get; set; } = string.Empty;
        
        // Dates
        public DateTime CreatedAt { get; set; }
        public DateTime ApprovedAt { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        
        // Additional info
        public string? Terms { get; set; }
        public string? Requirements { get; set; }
        public string? Notes { get; set; }
        public string? RescindReason { get; set; }
        
        // People involved
        public string? CreatedByStaffName { get; set; }
        public string? ApprovedByUserId { get; set; }
        public string? RespondedByGuardianUserId { get; set; }
        
        // Flags
        public bool RequiresGuardianApproval { get; set; }
    }
}
