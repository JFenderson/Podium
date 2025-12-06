using System.ComponentModel.DataAnnotations;

namespace Podium.Application.DTOs.BandStaff
{
    public class CreateBandStaffDto
    {
        [Required]
        public int BandId { get; set; }

        [Required]
        [MaxLength(450)]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        public bool CanContact { get; set; } = true;
        public bool CanMakeOffers { get; set; } = false;
        public bool CanViewFinancials { get; set; } = false;
        public bool CanViewStudents { get; set; } = true;
        public bool CanRateStudents { get; set; } = false;
        public bool CanSendOffers { get; set; } = false;
        public bool CanManageEvents { get; set; } = false;
        public bool CanManageStaff { get; set; } = false;
    }
}
