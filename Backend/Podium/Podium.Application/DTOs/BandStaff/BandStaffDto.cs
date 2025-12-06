using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    public class BandStaffDto
    {
        public int BandStaffId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }

        public bool IsActive { get; set; }
        public DateTime JoinedDate { get; set; }

        // Activity Metrics
        public int TotalContacts { get; set; }
        public int TotalOffersCreated { get; set; }
        public int SuccessfulPlacements { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
