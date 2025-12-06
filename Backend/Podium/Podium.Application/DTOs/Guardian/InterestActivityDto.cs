using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class InterestActivityDto
    {
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public DateTime InterestDate { get; set; }
        public bool HasBeenContacted { get; set; }
        public DateTime? ContactDate { get; set; }
    }
}
