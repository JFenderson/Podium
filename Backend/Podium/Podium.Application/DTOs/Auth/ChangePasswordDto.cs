using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Auth
{
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required")]
        [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
        [StringLength(100, ErrorMessage = "New password cannot exceed 100 characters")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password confirmation is required")]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
