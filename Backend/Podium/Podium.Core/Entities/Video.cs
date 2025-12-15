using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    public class Video : BaseEntity
    {
        [Required]
        public int StudentId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        public string Url { get; set; } = string.Empty;

        public string? ThumbnailUrl { get; set; }

        public bool IsPrimary { get; set; } = false;

        [Column(TypeName = "decimal(3,2)")]
        public decimal AverageRating { get; set; }

        // Navigation
        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }

        public bool IsDeleted { get; set; }

        public virtual ICollection<VideoRating> Ratings { get; set; } = new List<VideoRating>();
        public string Instrument { get; set; }
        public int ViewCount { get; set; }
        public bool IsPublic { get; set; }
        public string TranscodingStatus { get; set; }
        public string TranscodingError { get; set; }
        public DateTime CompletedAt { get; set; }
        public bool IsReviewed { get; set; }
    }
}