using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a request from a recruiter to contact a student.
    /// Requires guardian approval if student is a minor.
    /// </summary>
    public class ContactRequest : BaseEntity
    {


        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandId { get; set; }

        [Required]
        public int RecruiterStaffId { get; set; }

        [Required]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        [MaxLength(50)]
        public string PreferredContactMethod { get; set; } = "Email";

        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Approved, Declined

        public DateTime? ResponseDate { get; set; }

        [MaxLength(450)]
        public string? RespondedByGuardianUserId { get; set; }

        [MaxLength(500)]
        public string? ResponseNotes { get; set; }

        [MaxLength(500)]
        public string? DeclineReason { get; set; }

        public bool IsUrgent { get; set; } = false;

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;

        [ForeignKey(nameof(RecruiterStaffId))]
        public virtual BandStaff RecruiterStaff { get; set; } = null!;
        public string CreatedBy { get; set; }
    }
}