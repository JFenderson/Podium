using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    public class DirectorDashboardFiltersDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? RecruiterId { get; set; }
        public string? Instrument { get; set; }
        public string? OfferStatus { get; set; }
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }
}
