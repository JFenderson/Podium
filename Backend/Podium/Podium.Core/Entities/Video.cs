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
        /// <summary>
        /// Video category (e.g., "Performance", "Rehearsal", "Audition")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string? Category { get; set; }

        [Required]
        [MaxLength(100)]
        public string Instrument { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string VideoUrl { get; set; } = string.Empty;
        /// <summary>
        /// Original filename from upload
        /// </summary>
        [Required]
        [MaxLength(255)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Blob storage path/key for the video file
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string? BlobStoragePath { get; set; }

        /// <summary>
        /// Blob storage path for the thumbnail image
        /// </summary>
        [MaxLength(500)]
        public string? ThumbnailPath { get; set; }
        /// <summary>
        /// File size in bytes
        /// </summary>
        [Required]
        public long FileSizeBytes { get; set; }

        /// <summary>
        /// Video duration in seconds (populated after transcoding)
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// Transcoding status: Pending, Processing, Completed, Failed
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TranscodingStatus { get; set; } = "Pending";

        /// <summary>
        /// Error message if transcoding failed
        /// </summary>
        [MaxLength(1000)]
        public string? TranscodingError { get; set; }

        [MaxLength(500)]
        public string? ThumbnailUrl { get; set; }

        public int ViewCount { get; set; } = 0;

        public bool IsPublic { get; set; } = true;

        public bool IsReviewed { get; set; } = false;

        public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Average rating from recruiters (calculated field)
        /// </summary>
        public decimal? AverageRating { get; set; }

        /// <summary>
        /// Total number of ratings received
        /// </summary>
        public int RatingCount { get; set; } = 0;

        /// <summary>
        /// Soft delete flag - false means deleted
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date when the video was successfully uploaded and transcoding completed
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        // Navigation properties
        public virtual ICollection<VideoRating> Ratings { get; set; } = new List<VideoRating>();

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;
    }
}