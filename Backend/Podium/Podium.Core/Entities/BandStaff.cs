using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents the relationship between a band and its staff members (recruiters).
    /// Stores permissions and tracks activity for audit and analytics purposes.
    /// </summary>
    [Index(nameof(BandId), nameof(ApplicationUserId), IsUnique = true, Name = "IX_BandStaff_Band_User")]
    public class BandStaff
    {
        [Key]
        public int BandStaffId { get; set; }

        /// <summary>
        /// The band this staff member is associated with.
        /// </summary>
        [Required]
        public int BandId { get; set; }

        /// <summary>
        /// The user ID of the staff member (from Identity system).
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Role title (e.g., "Assistant Director", "Recruiting Coordinator", "Section Leader").
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if this staff member is currently active.
        /// Soft delete: Set to false instead of deleting to preserve audit trail.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Date when the staff member was added to the band.
        /// </summary>
        public DateTime JoinedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the staff member was deactivated (if applicable).
        /// </summary>
        public DateTime? DeactivatedDate { get; set; }

        // ============== PERMISSIONS ==============

        public bool CanViewStudents { get; set; } = true;
        public bool CanRateStudents { get; set; } = false;
        public bool CanSendOffers { get; set; } = false;
        public bool CanManageEvents { get; set; } = false;
        public bool CanManageStaff { get; set; } = false;

        // Additional permissions for new functionality
        public bool CanContact { get; set; } = true;
        public bool CanMakeOffers { get; set; } = false;
        public bool CanViewFinancials { get; set; } = false;

        // ============== ACTIVITY TRACKING ==============

        /// <summary>
        /// Total number of contact attempts made by this staff member.
        /// </summary>
        public int TotalContactsInitiated { get; set; } = 0;

        /// <summary>
        /// Total number of scholarship offers created by this staff member.
        /// </summary>
        public int TotalOffersCreated { get; set; } = 0;

        /// <summary>
        /// Number of students who accepted offers from this staff member.
        /// </summary>
        public int SuccessfulPlacements { get; set; } = 0;

        /// <summary>
        /// Last date/time when this staff member performed any action in the system.
        /// </summary>
        public DateTime? LastActivityDate { get; set; }

        // ============== AUDIT FIELDS ==============

        [Required]
        [MaxLength(450)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // ADD THIS - Alias for CreatedDate
        public DateTime? UpdatedAt { get; set; } // ADD THIS

        [MaxLength(450)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
        // Role differentiation
        public bool IsDirector { get; set; }
        public string Title { get; set; } = string.Empty;

        // ============== NAVIGATION PROPERTIES ==============

        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        [ForeignKey(nameof(BandId))]
        public virtual Band? Band { get; set; }

        public virtual ICollection<ContactLog> ContactsInitiated { get; set; } = new List<ContactLog>();
        public virtual ICollection<Offer> OffersCreated { get; set; } = new List<Offer>();
        public BandStaffPermissions Permissions { get; set; } = null!;
    }
}