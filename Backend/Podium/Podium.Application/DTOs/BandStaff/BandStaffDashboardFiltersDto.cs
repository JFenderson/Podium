using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Filters for band staff dashboard
    /// </summary>
    public class BandStaffDashboardFiltersDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Instrument { get; set; }
        public string? ContactStatus { get; set; }
    }
}
