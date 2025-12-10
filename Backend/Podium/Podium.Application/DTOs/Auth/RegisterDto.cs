using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Auth
{
    public class RegisterDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        // --- Role Selection ---
        [Required]
        // Expected values: "Student", "Guardian", "Recruiter", "Director"
        public string Role { get; set; } = string.Empty;

        // --- Student Specific Fields ---
        public string? Instrument { get; set; }
        public int? GraduationYear { get; set; }
        public string? HighSchool { get; set; }

        // --- Band Staff Specific Fields ---
        public int? BandId { get; set; }
        public string? StaffTitle { get; set; } // e.g., "Assistant Director", "Percussion Tech"
    }
}
