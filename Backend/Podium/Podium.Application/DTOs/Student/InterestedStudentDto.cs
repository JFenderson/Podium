using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    /// <summary>
    /// Student who has shown interest in the band.
    /// Includes engagement metrics and contact history.
    /// </summary>
    public class InterestedStudentDto
    {
        public int StudentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string PrimaryInstrument { get; set; } = string.Empty;
        public string SkillLevel { get; set; } = string.Empty;
        public int? GraduationYear { get; set; }
        public string HighSchool { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public DateTime InterestedDate { get; set; }

        // Engagement
        public int VideosUploaded { get; set; }
        public int EventsAttended { get; set; }
        public bool HasBeenContacted { get; set; }
        public DateTime? LastContactDate { get; set; }
        public bool HasOffer { get; set; }
        public string? OfferStatus { get; set; }

        // Guardian Info
        public bool HasGuardianLinked { get; set; }
        public bool RequiresGuardianApproval { get; set; }
    }
}
