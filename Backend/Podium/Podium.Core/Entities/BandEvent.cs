using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a band event (showcase, camp, audition, etc.)
    /// </summary>
    public class BandEvent
    {
        [Key]
        public int BandEventId { get; set; }

        [Required]
        public int BandId { get; set; }

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string EventType { get; set; } = string.Empty; // Showcase, Camp, Audition, InfoSession

        public DateTime EventDate { get; set; }

        public DateTime? EndDate { get; set; }

        [MaxLength(500)]
        public string? Location { get; set; }

        public int? CapacityLimit { get; set; }

        public bool IsRegistrationOpen { get; set; } = true;

        public DateTime? RegistrationDeadline { get; set; }

        public bool IsVirtual { get; set; } = false;

        [MaxLength(500)]
        public string? MeetingLink { get; set; }

        public bool IsArchived { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;

        public virtual ICollection<EventRegistration> Registrations { get; set; } = new List<EventRegistration>();
    }
}