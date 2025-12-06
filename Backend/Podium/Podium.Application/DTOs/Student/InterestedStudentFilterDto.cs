using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class InterestedStudentFilterDto
    {
        public string? Instrument { get; set; }
        public string? SkillLevel { get; set; }
        public int? GraduationYear { get; set; }
        public DateTime? InterestedAfter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
