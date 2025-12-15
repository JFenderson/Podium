using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Band
{
    public class BandSummaryDto
    {
        public int Id { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string? UniversityName { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ShortDescription { get; set; }
    }
}
