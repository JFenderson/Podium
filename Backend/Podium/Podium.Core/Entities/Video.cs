using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a student's audition video.
    /// </summary>
    public class Video
    {
        [Key]
        public int VideoId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Instrument { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string VideoUrl { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public int ViewCount { get; set; } = 0;

        public bool IsPublic { get; set; } = true;

        public bool IsReviewed { get; set; } = false;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;
    }
}