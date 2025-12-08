using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.DTOs.BandStaff;

namespace Podium.Application.DTOs.BandStaff
{
    public class BandStaffPermissionsDto
    {
        public string Role { get; set; } = string.Empty;
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }
        public bool IsActive { get; set; }
    }
}
