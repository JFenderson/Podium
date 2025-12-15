using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Podium.Core.Entities
{
    [Index(nameof(BandId), nameof(ApplicationUserId), IsUnique = true, Name = "IX_BandStaff_Band_User")]
    public class BandStaff : BaseEntity
    {
        public int BandId { get; set; }

        public string ApplicationUserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true; // Soft Delete Logic specific to Staff assignment
        public bool IsDirector { get; set; }

        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeactivatedDate { get; set; }

        // Permissions
        public bool CanViewStudents { get; set; } = true;
        public bool CanRateStudents { get; set; } = false;
        public bool CanSendOffers { get; set; } = false;
        public bool CanManageEvents { get; set; } = false;
        public bool CanManageStaff { get; set; } = false;
        public bool CanContact { get; set; } = true;
        public bool CanMakeOffers { get; set; } = false;
        public bool CanViewFinancials { get; set; } = false;

        // Activity Tracking
        public int TotalContactsInitiated { get; set; } = 0;
        public int TotalOffersCreated { get; set; } = 0;
        public int SuccessfulPlacements { get; set; } = 0;
        public DateTime? LastActivityDate { get; set; }

        // Specific Audit Fields (Not in BaseEntity)
        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        [MaxLength(450)]
        public string? ModifiedBy { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey(nameof(BandId))]
        public virtual Band? Band { get; set; }

        public virtual ICollection<ContactLog> ContactsInitiated { get; set; } = new List<ContactLog>();
        public virtual ICollection<ScholarshipOffer> OffersCreated { get; set; } = new List<ScholarshipOffer>();
    }
}