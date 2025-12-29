using Podium.Application.DTOs.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Complete band staff dashboard data
    /// </summary>
    public class BandStaffDashboardDto
    {
        public BandStaffPersonalMetricsDto PersonalMetrics { get; set; } = new();
        public List<MyStudentDto> MyStudents { get; set; } = new();
        public BandStaffPerformanceDto Performance { get; set; } = new();
        public List<MyActivityDto> RecentActivity { get; set; } = new();
        public List<MyPendingTaskDto> PendingTasks { get; set; } = new();
        public DateTime DateRangeStart { get; set; }
        public DateTime DateRangeEnd { get; set; }
    }
}
