using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a university marching band program.
    /// </summary>
    public class Band : BaseEntity
    {


        [Required]
        [StringLength(200)]
        public string BandName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? UniversityName { get; set; }
        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(50)]
        public string? State { get; set; }

        public string? Description { get; set; }

        public string? Achievements { get; set; }

        public string? DirectorApplicationUserId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ScholarshipBudget { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        // Soft Delete Property
        public bool IsDeleted { get; set; }
        // Concurrency Token
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Navigation properties
        [ForeignKey(nameof(DirectorApplicationUserId))]
        public virtual ApplicationUser? Director { get; set; }

        public virtual ICollection<BandStaff> Staff { get; set; } = new List<BandStaff>();
        public virtual ICollection<ScholarshipOffer> Offers { get; set; } = new List<ScholarshipOffer>();
        public virtual ICollection<BandEvent> Events { get; set; } = new List<BandEvent>();
        public virtual ICollection<BandBudget> Budgets { get; set; } = new List<BandBudget>();
        public bool IsHbcu { get; set; }
    }
}