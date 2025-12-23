using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Offer
{
    public class ScholarshipOverviewDto
    {
        public int TotalOffers { get; set; }
        public decimal TotalAmount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int AcceptedCount { get; set; }
        public int DeclinedCount { get; set; }
        public decimal AvailableBudget { get; set; }
        
        public List<ScholarshipOfferDto> Offers { get; set; } = new();
        
        // Pagination
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }



    public class RescindScholarshipRequest
    {
        [Required]
        [MinLength(10, ErrorMessage = "Please provide a detailed reason for rescinding this offer")]
        public string Reason { get; set; } = string.Empty;
    }
}
