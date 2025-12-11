using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class StudentDetailsDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Instrument { get; set; }
        public string? Bio { get; set; }
        public decimal? GPA { get; set; }

        // UX Enhancements
        public string? PhoneNumber { get; set; }
        public string? State { get; set; }
        public string? HighSchool { get; set; }
        public int GraduationYear { get; set; }
        public string? IntendedMajor { get; set; }
        public string? SkillLevel { get; set; }
        public string? SchoolType { get; set; }

        // Converted from JSON strings in Entity
        public List<string> SecondaryInstruments { get; set; } = new();
        public List<string> Achievements { get; set; } = new();
    }
}
