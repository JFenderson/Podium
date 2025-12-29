using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Logs actual contact made with a student (after approval).
    /// </summary>
    public class ContactLog : BaseEntity
    {
       

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int BandId { get; set; }

        [Required]
        public int BandStaffId { get; set; }


        [Required]
        [MaxLength(50)]
        public string ContactMethod { get; set; } = string.Empty; // Email, Phone, InPerson

        [MaxLength(500)]
        public string? Purpose { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        // Navigation properties
        [ForeignKey(nameof(StudentId))]
        public virtual Student Student { get; set; } = null!;

        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;

        [ForeignKey(nameof(BandStaffId))]
        public virtual BandStaff BandStaff { get; set; } = null!;
    }
}