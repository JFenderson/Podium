using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a university marching band program.
    /// </summary>
    public class Band
    {
        public int BandId { get; set; }

       
        public string? BandName { get; set; }

       
        public string? UniversityName { get; set; }
        public string? City { get; set; }

        public string? State { get; set; }

        public string? Description { get; set; }

        public string? Achievements { get; set; }

        /// <summary>
        /// Director's ApplicationUser ID
        /// </summary>
 
        public string? DirectorApplicationUserId { get; set; }

        public decimal ScholarshipBudget { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(DirectorApplicationUserId))]
        public virtual ApplicationUser? Director { get; set; }

        public virtual ICollection<BandStaff> Staff { get; set; } = new List<BandStaff>();
        public virtual ICollection<StudentInterest> StudentInterests { get; set; } = new List<StudentInterest>();
        public virtual ICollection<ScholarshipOffer> Offers { get; set; } = new List<ScholarshipOffer>();
        public virtual ICollection<BandEvent> Events { get; set; } = new List<BandEvent>();
    }
}