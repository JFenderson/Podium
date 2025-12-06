using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class OfferActivityDto
    {
        public int OfferId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime OfferDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool RequiresGuardianApproval { get; set; }
    }
}
