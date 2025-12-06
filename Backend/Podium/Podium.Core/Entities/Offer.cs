using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    public class Offer
    {
        public int OfferId { get; set; }
        public int StudentId { get; set; }
        public int CreatedByUserId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public decimal? ScholarshipAmount { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public int BandId { get; set; }
        public DateTime? ApprovedDate { get; set; }
        public DateTime? ResponseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? Terms { get; set; }
        public string? Requirements { get; set; }
        public bool RequiresGuardianApproval { get; set; } = true;
        public string? RescindReason { get; set; }
        public DateTime? ResponsedDate { get; set; }
        public string? RescindedByUserId { get; set; }
        public string? ResponseNotes { get; set; }
        public string? RespondedByGuardianUserId { get; set; }
        public int CreatedByStaffId { get; set; }
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(BandId))]
        public virtual Band? Band { get; set; }
        
        [ForeignKey(nameof(CreatedByStaffId))]
        public virtual BandStaff? CreatedByStaff { get; set; }
        
        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; } // ADD THIS
    }
}
