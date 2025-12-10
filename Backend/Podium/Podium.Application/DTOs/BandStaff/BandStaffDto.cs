using System;

namespace Podium.Application.DTOs.BandStaff
{
    public class BandStaffDto
    {
        public int BandStaffId { get; set; }
        public int BandId { get; set; }
        public string ApplicationUserId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        
        // Permissions
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }
        public bool CanContact { get; set; } public bool CanDelete { get; set; }
        public bool CanMakeOffers { get; set; }
        public bool CanViewFinancials { get; set; }
        
        // Activity metrics
        public int TotalContactsInitiated { get; set; }
        public int TotalOffersCreated { get; set; }
        public int SuccessfulPlacements { get; set; }
        
        // Dates
        public DateTime JoinedDate { get; set; }
        public DateTime? LastActivityDate { get; set; }
        public DateTime? DeactivatedDate { get; set; }
        public string? Email { get; set; }
        public string? Title { get; set; }
    }
}
