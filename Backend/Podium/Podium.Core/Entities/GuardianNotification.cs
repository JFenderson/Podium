using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a notification sent to a guardian.
    /// </summary>
    public class GuardianNotification
    {
        [Key]
        public int GuardianNotificationId { get; set; }

        [Required]
        [MaxLength(450)]
        public string GuardianApplicationUserId { get; set; } = string.Empty;

        public int? StudentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty; // NewOffer, ContactRequest, OfferExpiring, StudentActivity

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;

        public bool IsUrgent { get; set; } = false;

        [MaxLength(500)]
        public string? ActionUrl { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student? Student { get; set; }
    }
}