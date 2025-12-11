using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class UpdateStudentDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? Instrument { get; set; } // Primary Instrument

        // Full Profile Management Fields
        public string? PhoneNumber { get; set; }
        public string? State { get; set; }
        public string? HighSchool { get; set; }
        public int? GraduationYear { get; set; }
        public string? IntendedMajor { get; set; }
        public string? SkillLevel { get; set; }
        public string? SchoolType { get; set; }

        // Lists to be serialized to JSON
        public List<string>? SecondaryInstruments { get; set; }
        public List<string>? Achievements { get; set; }
    }
}
