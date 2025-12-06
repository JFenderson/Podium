using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandEvent
{
    public class EventFilterDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? EventType { get; set; }
        public bool IncludeArchived { get; set; } = false;
    }
}
