using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Band
{
    public class BandStaffSummaryDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
