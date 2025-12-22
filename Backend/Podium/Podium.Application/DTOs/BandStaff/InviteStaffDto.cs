using System.ComponentModel.DataAnnotations;

namespace Podium.Application.DTOs.BandStaff
{
    public class InviteStaffDto
    {
        [Required]
        public int BandId { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string? Title { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        // Default Permissions for the invite
        public bool CanContact { get; set; } = true;
        public bool CanViewStudents { get; set; } = true;
        public bool CanMakeOffers { get; set; } = false;
        public bool CanViewFinancials { get; set; } = false;
        public bool CanRateStudents { get; set; } = false;
        public bool CanSendOffers { get; set; } = false;
        public bool CanManageEvents { get; set; } = false;
        public bool CanManageStaff { get; set; } = false;
    }
}