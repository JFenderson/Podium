using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Scholarship offer from guardian perspective.
    /// Includes details needed for decision-making.
    /// </summary>
    public class GuardianScholarshipDto
    {
        public int OfferId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime OfferDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public int DaysUntilExpiration { get; set; }
        public string? Terms { get; set; }
        public string? Requirements { get; set; }
        public bool RequiresGuardianApproval { get; set; }
        public bool CanRespond { get; set; } // Based on guardian permissions
        public string? RecruiterName { get; set; }
        public string? RecruiterEmail { get; set; }
        public string? RecruiterPhone { get; set; }

        // Additional context for decision
        public string? BandDescription { get; set; }
        public string? BandAchievements { get; set; }
    }

}
