using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class SearchFilterCriteria
    {
        // Instruments
        public List<string>? Instruments { get; set; }

        // Location
        public string? State { get; set; }
        public bool? HbcuOnly { get; set; }
        public double? DistanceRadius { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ZipCode { get; set; }

        // Academics
        public double? MinGpa { get; set; }
        public double? MaxGpa { get; set; }
        public int? GraduationYear { get; set; }
        public List<string>? Majors { get; set; }

        // Experience
        public List<string>? SkillLevels { get; set; } // Beginner, Intermediate, Advanced, Expert
        public int? MinYearsExperience { get; set; }
        public int? MaxYearsExperience { get; set; }
        public bool? HasVideo { get; set; }
        public bool? HasAudio { get; set; }

        // Engagement
        public bool? ShowInterested { get; set; }
        public bool? ShowContacted { get; set; }
        public bool? ShowRated { get; set; }
        public int? MinRating { get; set; }
        public int? MaxRating { get; set; }

        // Sorting
        public string? SortBy { get; set; } // gpa, experience, rating, recent
        public string? SortDirection { get; set; } // asc, desc
    }
}
