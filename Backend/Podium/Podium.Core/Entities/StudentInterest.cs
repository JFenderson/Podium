using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a student's interest in a specific band.
    /// </summary>
    [Index(nameof(StudentId), nameof(BandId), IsUnique = true)]
    public class StudentInterest
    {
        [Key]
        public int StudentInterestId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandId { get; set; }
        public bool IsInterested { get; set; } = true;

        public DateTime InterestedDate { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;
    }
}