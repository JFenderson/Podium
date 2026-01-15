using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class StudentCardDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Instrument { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public decimal? GPA { get; set; }
        public int GraduationYear { get; set; }
        public int VideoCount { get; set; }
        public string? ProfilePhotoUrl { get; set; }
    }
}
