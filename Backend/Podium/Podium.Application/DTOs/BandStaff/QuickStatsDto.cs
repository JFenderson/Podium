using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Quick stats summary
    /// </summary>
    public class QuickStatsDto
    {
        public int ActiveStudents { get; set; }
        public int PendingContacts { get; set; }
        public int PendingOffers { get; set; }
        public decimal BudgetRemaining { get; set; }
    }
}
