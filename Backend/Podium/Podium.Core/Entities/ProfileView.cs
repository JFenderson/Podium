using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    public class ProfileView : BaseEntity
    {
      

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandId { get; set; }

        [MaxLength(450)]
        public string? ViewerUserId { get; set; } = string.Empty;

        public DateTime ViewedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;

        [ForeignKey(nameof(ViewerUserId))]
        public virtual ApplicationUser? ViewerUser { get; set; }
    }
}