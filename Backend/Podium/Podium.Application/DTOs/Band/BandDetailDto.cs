using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Band
{
    public class BandDetailDto
    {
        public int Id { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string? UniversityName { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? Description { get; set; }
        public string? Achievements { get; set; }

        // Engagement Metrics for display
        public int UpcomingEventsCount { get; set; }
        public bool HasScholarshipsAvailable { get; set; }
    }
}
