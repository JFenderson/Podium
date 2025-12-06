using System.ComponentModel.DataAnnotations;

namespace Podium.Application.DTOs.BandStaff
{
    public class UpdateBandStaffDto
    {
        [Required]
        [StringLength(50)]
        public string Role { get; set; } = string.Empty;

        public bool CanContact { get; set; }
        public bool CanMakeOffers { get; set; }
        public bool CanViewFinancials { get; set; }
        public bool CanViewStudents { get; set; }
        public bool CanRateStudents { get; set; }
        public bool CanSendOffers { get; set; }
        public bool CanManageEvents { get; set; }
        public bool CanManageStaff { get; set; }
        public bool IsActive { get; set; } = true;
    }
}