using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a university marching band program.
    /// </summary>
    public class Band
    {
        [Key]
        public int BandId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(200)]
        public string UniversityName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? Achievements { get; set; }

        /// <summary>
        /// Director's ApplicationUser ID
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string DirectorApplicationUserId { get; set; } = string.Empty;

        public decimal ScholarshipBudget { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(DirectorApplicationUserId))]
        public virtual ApplicationUser? Director { get; set; }

        public virtual ICollection<BandStaff> Staff { get; set; } = new List<BandStaff>();
        public virtual ICollection<StudentInterest> StudentInterests { get; set; } = new List<StudentInterest>();
        public virtual ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public virtual ICollection<BandEvent> Events { get; set; } = new List<BandEvent>();
    }
}