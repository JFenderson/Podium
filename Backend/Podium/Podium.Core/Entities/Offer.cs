using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class Offer
    {
        public int OfferId { get; set; }
        public int StudentId { get; set; }
        public int CreatedByUserId { get; set; }
        public string OfferType { get; set; } = string.Empty;
        public decimal? ScholarshipAmount { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? ApprovedByUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }
}
