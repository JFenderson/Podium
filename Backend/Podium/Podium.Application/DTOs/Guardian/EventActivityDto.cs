using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class EventActivityDto
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public bool DidAttend { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
}
