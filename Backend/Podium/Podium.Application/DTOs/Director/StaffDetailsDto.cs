using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    public class StaffDetailsDto
    {
        public int StaffId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string? Title { get; set; }
        public string BandName { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public decimal BudgetAllocation { get; set; }

        // Permissions
        public bool CanContact { get; set; }
        public bool CanViewStudents { get; set; }
        public bool CanMakeOffers { get; set; }
        public bool CanViewFinancials { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }
    }
}
