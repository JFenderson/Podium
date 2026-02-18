using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Podium.Application.Validation;

namespace Podium.Application.DTOs.Student
{
    public class UpdateStudentDto
    {
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

        public DateTime? DateOfBirth { get; set; }

        [StringLength(2000, ErrorMessage = "Bio description cannot exceed 2000 characters")]
        public string? BioDescription { get; set; }

        // Education
        [GraduationYear]
        public int? GraduationYear { get; set; }

        [StringLength(100, ErrorMessage = "High school name cannot exceed 100 characters")]
        [SafeString]
        public string? HighSchool { get; set; }

        [GPA]
        public decimal? GPA { get; set; }

        [StringLength(100, ErrorMessage = "Intended major cannot exceed 100 characters")]
        [SafeString]
        public string? IntendedMajor { get; set; }

        [StringLength(50, ErrorMessage = "School type cannot exceed 50 characters")]
        [SafeString]
        public string? SchoolType { get; set; }

        // Music
        [Instrument]
        public string? PrimaryInstrument { get; set; }

        [StringLength(50, ErrorMessage = "Skill level cannot exceed 50 characters")]
        [SafeString]
        public string? SkillLevel { get; set; }

        [Range(0, 50, ErrorMessage = "Years of experience must be between 0 and 50")]
        public int? YearsExperience { get; set; }

        // Collections (Flexible types)
        public List<string>? SecondaryInstruments { get; set; }
        public List<string>? Awards { get; set; }
        public List<string>? Achievements { get; set; }

        // Location
        [StringLength(2, MinimumLength = 2, ErrorMessage = "State must be a 2-letter code")]
        [RegularExpression("^[A-Za-z]{2}$", ErrorMessage = "State must be a valid 2-letter US state code")]
        public string? State { get; set; }

        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        [SafeString]
        public string? City { get; set; }

        [RegularExpression(@"^\d{5}(-\d{4})?$", ErrorMessage = "Zipcode must be in format 12345 or 12345-6789")]
        public string? Zipcode { get; set; }
    }
}