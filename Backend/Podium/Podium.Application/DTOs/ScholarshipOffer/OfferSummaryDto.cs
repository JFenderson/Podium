using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.ScholarshipOffer
{
    public class OfferSummaryDto
    {
        public int Id { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string? UniversityName { get; set; }
        public string? Location { get; set; } // City, State combined
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string OfferType { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
    }
}
