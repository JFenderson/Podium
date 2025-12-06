using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Tracks when recruiters view student profiles for analytics.
    /// </summary>
    public class ProfileView
    {
        [Key]
        public int ProfileViewId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandId { get; set; }

        [Required]
        [MaxLength(450)]
        public string ViewedByApplicationUserId { get; set; } = string.Empty;

        public DateTime ViewedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;
    }
}