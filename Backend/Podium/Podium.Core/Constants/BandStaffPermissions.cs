using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Permissions for band staff members (Recruiters and Directors)
    /// </summary>
    [Table("BandStaffPermissions")]
    public class BandStaffPermissions
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [ForeignKey(nameof(ApplicationUserId))]
        public ApplicationUser ApplicationUser { get; set; } = null!;

        // Student-related permissions
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanContactStudents { get; set; }

        // Offer-related permissions
        public bool CanSendOffers { get; set; }
        public bool CanManageOffers { get; set; }

        // Event-related permissions
        public bool CanManageEvents { get; set; }

        // Staff-related permissions (Director only)
        public bool CanManageStaff { get; set; }

        // Band-related permissions (Director only)
        public bool CanManageBand { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}