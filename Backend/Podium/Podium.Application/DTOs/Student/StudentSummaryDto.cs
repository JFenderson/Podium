using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public int? GraduationYear { get; set; }
        public string? PrimaryInstrument { get; set; }
        public string? HighSchool { get; set; }
        public string? VideoThumbnailUrl { get; set; }
        public double? AverageRating { get; set; }
        public int? RatingCount { get; set; }
    }
}
