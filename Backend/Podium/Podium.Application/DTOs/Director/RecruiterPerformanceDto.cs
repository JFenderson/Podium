using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    // Response for: GET band/{bandId}/staff-performance
    public class RecruiterPerformanceDto
    {
        public int StaffId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ContactsInitiated { get; set; }
        public int OffersSent { get; set; }
        public int SuccessfulPlacements { get; set; } // Accepted offers
        public decimal ConversionRate { get; set; }   // Offers Accepted / Offers Sent
    }
}
