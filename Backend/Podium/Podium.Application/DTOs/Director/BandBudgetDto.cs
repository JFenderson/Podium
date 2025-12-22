using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    // Response for: GET band/{bandId}/scholarship-budget
    public class BandBudgetDto
    {
        public decimal TotalBudget { get; set; }
        public decimal Allocated { get; set; }         // Money promised (Sent + Accepted)
        public decimal Remaining { get; set; }
        public decimal PendingCommitment { get; set; } // Money in "Sent" but not yet "Accepted"
        public int FiscalYear { get; set; }
    }
}
