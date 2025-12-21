using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Student profile with information guardian has permission to view.
    /// Respects student privacy settings.
    /// </summary>
    public class LinkedStudentProfileViewDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Bio { get; set; }

        // Academic info
        public string HighSchool { get; set; } = string.Empty;
        public int GraduationYear { get; set; }
        public decimal? GPA { get; set; }
        public string? IntendedMajor { get; set; }

        // Musical background
        public string PrimaryInstrument { get; set; } = string.Empty;
        public List<string> SecondaryInstruments { get; set; } = new();
        public string SkillLevel { get; set; } = string.Empty;
        public int YearsExperience { get; set; }
        public List<string> Achievements { get; set; } = new();

        // Engagement summary
        public int VideosUploaded { get; set; }
        public int BandsInterested { get; set; }
        public int EventsAttended { get; set; }
        public DateTime? LastActivityDate { get; set; }
    }
}
