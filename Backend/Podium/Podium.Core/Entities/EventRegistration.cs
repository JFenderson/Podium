using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a student's registration for a band event.
    /// </summary>
    [Index(nameof(StudentId), nameof(EventId), IsUnique = true)]
    public class EventRegistration
    {
        [Key]
        public int EventRegistrationId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int EventId { get; set; }

        public DateTime RegisteredDate { get; set; } = DateTime.UtcNow;

        public bool DidAttend { get; set; } = false;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(EventId))]
        public virtual BandEvent Event { get; set; } = null!;
    }
}