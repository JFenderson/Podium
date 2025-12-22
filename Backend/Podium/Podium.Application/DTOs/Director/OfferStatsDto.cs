using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    // Response for: GET band/{bandId}/offer-stats
    public class OfferStatsDto
    {
        public int TotalOffers { get; set; }
        public int Pending { get; set; }
        public int Accepted { get; set; }
        public int Declined { get; set; }
        public int Expired { get; set; }
        public decimal AcceptanceRate { get; set; } // e.g. 45.5
        public decimal ResponseRate { get; set; }   // e.g. 80.0
    }
}
