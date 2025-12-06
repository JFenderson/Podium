using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class BandStaff
    {
        [Key]
        public int BandStaffId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty; // "Recruiter" or "Director"

        // Permission flags
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }

        // Navigation property
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}
