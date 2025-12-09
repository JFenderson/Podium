using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a recruiter's rating of a student video
    /// </summary>
    public class VideoRating
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VideoId { get; set; }

        [ForeignKey(nameof(VideoId))] // Use nameof for safety
        public virtual Video Video { get; set; } = null!;

        [Required]
        public int BandStaffId { get; set; }

        [ForeignKey(nameof(BandStaffId))]
        public virtual BandStaff BandStaff { get; set; } = null!;

        /// <summary>
        /// Rating value from 1-5
        /// </summary>
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }
    }
}