using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Offer
{
    public class CreateScholarshipOfferDto
    {
        public int StudentId { get; set; }
        public int BandId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public DateTime ExpirationDate { get; set; }
        public bool RequiresGuardianApproval { get; set; }
    }
}
