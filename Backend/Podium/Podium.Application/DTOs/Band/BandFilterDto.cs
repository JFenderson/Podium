using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Band
{
    public class BandFilterDto
    {
        public string? Search { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
    }
}
