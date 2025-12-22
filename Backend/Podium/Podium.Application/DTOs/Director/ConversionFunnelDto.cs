using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    // Response for: GET band/{bandId}/conversion-funnel
    public class ConversionFunnelDto
    {
        public int TotalInterests { get; set; }    // Top: Interested
        public int Contacted { get; set; }         // Middle: Contacted
        public int OffersSent { get; set; }        // Bottom: Offer Made
        public int OffersAccepted { get; set; }    // Success: Committed

        // Calculated Rates
        public decimal InterestToContactRate => TotalInterests > 0 ? (decimal)Contacted / TotalInterests * 100 : 0;
        public decimal ContactToOfferRate => Contacted > 0 ? (decimal)OffersSent / Contacted * 100 : 0;
        public decimal OfferToAcceptRate => OffersSent > 0 ? (decimal)OffersAccepted / OffersSent * 100 : 0;
    }
}
