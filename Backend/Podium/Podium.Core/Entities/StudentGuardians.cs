using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents the relationship between a student and their parent/guardian.
    /// Stores permissions that control what guardians can see and do regarding the student's recruitment.
    /// </summary>
    [Table("StudentGuardians")]
    [Index(nameof(StudentId), nameof(GuardianUserId), IsUnique = true, Name = "IX_StudentGuardian_Student_Guardian")]
    [Index(nameof(GuardianUserId), Name = "IX_StudentGuardian_Guardian")]
    public class StudentGuardian
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// The student associated with this guardian link.
        /// </summary>
        [Required]
        public int StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        /// <summary>
        /// The user ID of the guardian (from Identity system).
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string GuardianUserId { get; set; } = string.Empty;

        /// <summary>
        /// Type of relationship (e.g., "Parent", "Guardian", "Other").
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string RelationshipType { get; set; } = "Parent";

        /// <summary>
        /// Indicates if this guardian link has been verified.
        /// Verification may involve email confirmation, document upload, or other validation.
        /// Only verified guardians can approve sensitive actions.
        /// </summary>
        public bool IsVerified { get; set; } = false;

        /// <summary>
        /// Date when the guardian was linked to the student.
        /// </summary>
        public DateTime LinkedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date when the link was verified (if applicable).
        /// </summary>
        public DateTime? VerifiedDate { get; set; }

        /// <summary>
        /// Indicates if this guardian link is currently active.
        /// Can be deactivated if student removes guardian access.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ============== PERMISSIONS ==============
        // Students can grant different levels of access to different guardians

        /// <summary>
        /// Permission to view student's activity (videos, interests, contacts).
        /// Basic monitoring permission.
        /// </summary>
        public bool CanViewActivity { get; set; } = true;

        /// <summary>
        /// Permission to approve/decline contact requests from recruiters.
        /// Required for students under 18 in many jurisdictions.
        /// </summary>
        public bool CanApproveContacts { get; set; } = true;

        /// <summary>
        /// Permission to accept or decline scholarship offers on behalf of the student.
        /// Typically granted to parents/guardians who will be financially involved.
        /// </summary>
        public bool CanRespondToOffers { get; set; } = false;

        /// <summary>
        /// Permission to view the student's profile and personal information.
        /// </summary>
        public bool CanViewProfile { get; set; } = true;

        /// <summary>
        /// Permission to modify notification preferences for this student.
        /// </summary>
        public bool CanManageNotifications { get; set; } = true;

        /// <summary>
        /// Whether guardian receives notifications about this student's activity.
        /// </summary>
        public bool ReceivesNotifications { get; set; } = true;

        // ============== CONTACT INFORMATION ==============
        // Stored here for communication purposes; may differ from user account

        [MaxLength(100)]
        public string? GuardianName { get; set; }

        [MaxLength(255)]
        [EmailAddress]
        public string? GuardianEmail { get; set; }

        [MaxLength(20)]
        [Phone]
        public string? GuardianPhone { get; set; }

        // ============== AUDIT FIELDS ==============

        [MaxLength(450)]
        public string? CreatedBy { get; set; } // Usually the student themselves

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? ModifiedBy { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // ============== VERIFICATION METADATA ==============

        /// <summary>
        /// Method used to verify this guardian relationship.
        /// </summary>
        [MaxLength(50)]
        public string? VerificationMethod { get; set; } // "Email", "Document", "SchoolRecord", etc.

        /// <summary>
        /// User ID who verified this relationship (if not auto-verified).
        /// </summary>
        [MaxLength(450)]
        public string? VerifiedBy { get; set; }

        /// <summary>
        /// Additional verification data (e.g., document IDs, confirmation codes).
        /// Stored as JSON string for flexibility.
        /// </summary>
        [MaxLength(1000)]
        public string? VerificationMetadata { get; set; }
    }

}
