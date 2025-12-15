using Podium.Core.Constants;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{

    public class ScholarshipOffer : BaseEntity
    {
  
        public int StudentId { get; set; }
        public int BandId { get; set; }

        // Workflow State
        public ScholarshipStatus Status { get; set; } = ScholarshipStatus.Draft;

        // Financials
        [Column(TypeName = "decimal(18,2)")]
        public decimal ScholarshipAmount { get; set; }
        public string OfferType { get; set; } = "Scholarship";
        public string? Description { get; set; }
        public string? Terms { get; set; }
        public string? Requirements { get; set; } 
        public DateTime ExpirationDate { get; set; }

        // Audit Trail
        public string? CreatedByUserId { get; set; }

        public int? CreatedByStaffId { get; set; }


        public string? ApprovedByUserId { get; set; }
        public DateTime ApprovedAt { get; set; }

        public string? RescindedByUserId { get; set; }
        public string? RescindReason { get; set; }
        public DateTime? RescindedDate { get; set; }

        // Response Data
        public string? RespondedByUserId { get; set; } // Could be Student or Guardian
        public string? RespondedByGuardianUserId { get; set; }
        public bool RespondedByGuardian { get; set; }
        public string? ResponseNotes { get; set; }
        public DateTime? ResponseDate { get; set; }

        public bool RequiresGuardianApproval { get; set; } = true;

        // Navigation
        [ForeignKey(nameof(BandId))]
        public virtual Band? Band { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        [ForeignKey(nameof(CreatedByStaffId))]
        public virtual BandStaff? CreatedByStaff { get; set; }
        public virtual ApplicationUser? ApprovedByUser { get; set; }
        public DateTime ApprovedDate { get; set; }
    }
}