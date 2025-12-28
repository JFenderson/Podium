using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    public class StudentSearchResultDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public string PrimaryInstrument { get; set; }
        public string[]? SecondaryInstruments { get; set; }
        public string SkillLevel { get; set; }
        public int YearsOfExperience { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string? ZipCode { get; set; }
        public double? GPA { get; set; }
        public int GraduationYear { get; set; }
        public string? IntendedMajor { get; set; }
        public string? HighSchool { get; set; }
        public int VideoCount { get; set; }
        public bool HasAuditionVideo { get; set; }
        public int ProfileViews { get; set; }
        public double? AverageRating { get; set; }
        public int RatingCount { get; set; }
        public string AccountStatus { get; set; }
        public bool IsAvailableForRecruiting { get; set; }
        public DateTime LastActivityDate { get; set; }
        public bool IsWatchlisted { get; set; }
        public bool HasSentOffer { get; set; }
        public bool HasContactRequest { get; set; }
        public double? MatchScore { get; set; }
    }
}
