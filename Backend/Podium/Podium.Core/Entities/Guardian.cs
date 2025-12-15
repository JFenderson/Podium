using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    public class Guardian : BaseEntity
    {
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

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        // Preferences
        public bool EmailNotificationsEnabled { get; set; }
        public bool SmsNotificationsEnabled { get; set; }

        // Navigation
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        public virtual ICollection<StudentGuardian>? StudentLinks { get; set; }
        public virtual ICollection<Student> Students { get; set; } = new List<Student>();
    }
}