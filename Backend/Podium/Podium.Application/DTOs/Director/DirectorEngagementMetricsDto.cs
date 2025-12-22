using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    // Response for: GET band/{bandId}/engagement
    public class DirectorEngagementMetricsDto
    {
        public int TotalProfileViews { get; set; }
        public int TotalVideoWatches { get; set; }
        public int TotalInterests { get; set; }
        public List<DailyEngagementDto> DailyActivity { get; set; } = new();
    }
}
