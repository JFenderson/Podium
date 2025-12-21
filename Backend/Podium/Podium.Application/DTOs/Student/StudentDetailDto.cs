using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.Student
{
    public class StudentDetailsDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Music Profile
        public string? PrimaryInstrument { get; set; }
        public string? Bio { get; set; }
        public string? BioDescription => Bio; // Frontend alias

        // Academics & Personal
        public decimal? GPA { get; set; }
        public DateTime? DateOfBirth { get; set; } // Note: Ensure Entity has this or ignore
        public int? GraduationYear { get; set; }
        public string? HighSchool { get; set; }
        public string? IntendedMajor { get; set; }
        public string? SkillLevel { get; set; }
        public string? SchoolType { get; set; }
        public int? YearsExperience { get; set; }

        // Contact / Location
        public string? PhoneNumber { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }     // Note: Add to Student Entity if missing
        public string? Zipcode { get; set; }  // Note: Add to Student Entity if missing

        // Computed / Collections
        public string? VideoUrl { get; set; }
        public string? VideoThumbnailUrl { get; set; }
        public double? AverageRating { get; set; }
        public int? RatingCount { get; set; }
        public int? ProfileViews { get; set; }
        public bool HasGuardian { get; set; }

        public List<string> SecondaryInstruments { get; set; } = new();
        public List<string> Achievements { get; set; } = new();
        public List<string> Awards => Achievements; // Frontend alias

        // Richer object for Interests
        public List<StudentInterestDetailDto> Interests { get; set; } = new();

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}