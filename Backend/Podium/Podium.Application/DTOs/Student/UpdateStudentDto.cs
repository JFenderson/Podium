using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.Student
{
    public class UpdateStudentDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }

        public string? BioDescription { get; set; } // Maps to Bio

        // Education
        public int? GraduationYear { get; set; }
        public string? HighSchool { get; set; }
        public decimal? GPA { get; set; }
        public string? IntendedMajor { get; set; }
        public string? SchoolType { get; set; }

        // Music
        public string? PrimaryInstrument { get; set; }
        public string? SkillLevel { get; set; }
        public int? YearsExperience { get; set; }

        // Collections (Flexible types)
        public List<string>? SecondaryInstruments { get; set; }
        public List<string>? Awards { get; set; } // Maps to Achievements
        public List<string>? Achievements { get; set; }

        // Location
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Zipcode { get; set; }
    }
}