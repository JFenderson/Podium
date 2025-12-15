using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    public class Notification : BaseEntity
    {


        [Required]
        public string UserId { get; set; } = string.Empty; // FK to ApplicationUser

        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = string.Empty; // e.g., "ScholarshipOffer", "NewMessage"

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string Message { get; set; } = string.Empty;

        public string? RelatedEntityId { get; set; } // e.g., OfferId, VideoId

        public bool IsRead { get; set; } = false;

    }
}