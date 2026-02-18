using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.Validation;

namespace Podium.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        [StringLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "First name is required")]
        [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
        [SafeString]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required")]
        [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
        [SafeString]
        public string LastName { get; set; } = string.Empty;

        [PhoneNumber]
        public string? PhoneNumber { get; set; }

        // --- Role Selection ---
        [Required(ErrorMessage = "Role is required")]
        [RegularExpression("^(Student|Guardian|Recruiter|Director)$", ErrorMessage = "Role must be Student, Guardian, Recruiter, or Director")]
        public string Role { get; set; } = string.Empty;

        // --- Student Specific Fields ---
        [Instrument]
        public string? Instrument { get; set; }
        
        [GraduationYear]
        public int? GraduationYear { get; set; }
        
        [StringLength(100, ErrorMessage = "High school name cannot exceed 100 characters")]
        [SafeString]
        public string? HighSchool { get; set; }

        // --- Band Staff Specific Fields ---
        [Range(1, int.MaxValue, ErrorMessage = "Band ID must be a positive number")]
        public int? BandId { get; set; }
        
        [StringLength(100, ErrorMessage = "Staff title cannot exceed 100 characters")]
        [SafeString]
        public string? StaffTitle { get; set; }
    }
}
