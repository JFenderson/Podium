using Podium.Application.DTOs.Director;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Guardian's comprehensive dashboard view of all linked students.
    /// Single optimized query with strategic projections.
    /// </summary>
    public class GuardianDashboardDto
    {
        public List<GuardianLinkedStudentDto> LinkedStudents { get; set; } = new();

        public int TotalPendingApprovals { get; set; }
        public int TotalActiveOffers { get; set; }
        public int TotalUnreadNotifications { get; set; }

        public List<PriorityAlertDto> PriorityAlerts { get; set; } = new();
        public List<GuardianRecentActivityDto> RecentActivities { get; set; } = new();
    }
}
