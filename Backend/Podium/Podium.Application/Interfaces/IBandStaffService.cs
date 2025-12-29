using Podium.Application.DTOs.BandStaff;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IBandStaffService
    {
        /// <summary>
        /// Get complete dashboard for band staff member
        /// </summary>
        Task<BandStaffDashboardDto> GetDashboardAsync(string staffUserId, BandStaffDashboardFiltersDto filters);

        /// <summary>
        /// Get personal metrics for band staff member
        /// </summary>
        Task<BandStaffPersonalMetricsDto> GetPersonalMetricsAsync(string staffUserId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get students this staff member is recruiting
        /// </summary>
        Task<List<MyStudentDto>> GetMyStudentsAsync(string staffUserId, string? filterStatus = null);

        /// <summary>
        /// Get performance metrics for this staff member
        /// </summary>
        Task<BandStaffPerformanceDto> GetMyPerformanceAsync(string staffUserId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get recent activity for this staff member
        /// </summary>
        Task<List<MyActivityDto>> GetMyActivityAsync(string staffUserId, int limit = 20);

        /// <summary>
        /// Get pending tasks for this staff member
        /// </summary>
        Task<List<MyPendingTaskDto>> GetMyPendingTasksAsync(string staffUserId);

        /// <summary>
        /// Get quick stats summary
        /// </summary>
        Task<QuickStatsDto> GetQuickStatsAsync(string staffUserId);

        /// <summary>
        /// Search students with filters (for this staff member's context)
        /// </summary>
        Task<StaffStudentSearchDto> SearchStudentsAsync(string staffUserId, string? searchTerm, int page = 1, int pageSize = 20);
    }
}
