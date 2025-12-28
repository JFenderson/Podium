using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    public class ExportOptionsDto
    {
        public string Format { get; set; } // csv, excel, pdf
        public bool IncludeCharts { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> Sections { get; set; } // metrics, funnel, offers, staff, approvals
    }
}
