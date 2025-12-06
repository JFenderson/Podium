using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Instrument { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? GPA { get; set; }

        // Navigation property
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        // Navigation property for guardian relationship
        public virtual ICollection<Guardian>? Guardians { get; set; }
    }
}
