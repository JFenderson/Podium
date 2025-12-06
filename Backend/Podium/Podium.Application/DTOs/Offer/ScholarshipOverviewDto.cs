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
        public List<ScholarshipOfferDto> Offers { get; set; } = new();
        public int TotalCount { get; set; }
        public decimal TotalAmount { get; set; }
        public ScholarshipBudgetDto BudgetSummary { get; set; } = new();
    }

    public class ApproveScholarshipRequest
    {
        public string? Notes { get; set; }
    }

    public class RescindScholarshipRequest
    {
        [Required]
        [MinLength(10, ErrorMessage = "Please provide a detailed reason for rescinding this offer")]
        public string Reason { get; set; } = string.Empty;
    }
}
