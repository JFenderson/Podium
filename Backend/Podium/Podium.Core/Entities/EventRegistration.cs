using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Represents a student's registration for a band event.
    /// </summary>
    [Index(nameof(StudentId), nameof(BandEventId), IsUnique = true)]
    public class EventRegistration : BaseEntity
    {
        

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandEventId { get; set; }


        public bool DidAttend { get; set; } = false;

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandEventId))]
        public virtual BandEvent BandEvent { get; set; } = null!;
    }
}