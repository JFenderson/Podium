// IDirectorService.cs - MERGED Interface
// Backend/Podium/Podium.Application/Interfaces/IDirectorService.cs

using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandEvent;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;

namespace Podium.Application.Interfaces
{
    public interface IDirectorService
    {
        // ==========================================
        // DASHBOARD & ANALYTICS (NEW)
        // ==========================================

        /// <summary>
        /// Get complete director dashboard with all metrics
        /// </summary>
        Task<DirectorDashboardDto> GetDashboardAsync(string directorUserId, DirectorDashboardFiltersDto filters);

        /// <summary>
        /// Get key metrics only (lightweight call)
        /// </summary>
        Task<DirectorKeyMetricsDto> GetKeyMetricsAsync(string directorUserId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get recruitment funnel data
        /// </summary>
        Task<List<FunnelStageDto>> GetRecruitmentFunnelAsync(int bandId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get students in a specific funnel stage
        /// </summary>
        Task<List<FunnelStudentDto>> GetFunnelStageStudentsAsync(int bandId, string stage, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get offers overview with time series and breakdowns
        /// </summary>
        Task<OffersOverviewDto> GetOffersOverviewAsync(int bandId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get staff performance metrics
        /// </summary>
        Task<List<StaffPerformanceDto>> GetStaffPerformanceAsync(int bandId, DateTime startDate, DateTime endDate);

        /// <summary>
        /// Get detailed staff information
        /// </summary>
        Task<StaffDetailsDto> GetStaffDetailsAsync(int staffId);

        /// <summary>
        /// Update staff budget allocation
        /// </summary>
        Task UpdateStaffBudgetAsync(int staffId, decimal newBudget, string updatedBy);

        /// <summary>
        /// Update staff permissions
        /// </summary>
        Task UpdateStaffPermissionsAsync(int staffId, BandStaffPermissionsDto permissions, string updatedBy);

        /// <summary>
        /// Get recent activity feed
        /// </summary>
        Task<List<ActivityItemDto>> GetRecentActivityAsync(int bandId, int limit);

        /// <summary>
        /// Export dashboard data
        /// </summary>
        Task<byte[]> ExportDashboardAsync(ExportOptionsDto options);

        // ==========================================
        // LEGACY DASHBOARD (EXISTING)
        // ==========================================

        /// <summary>
        /// Get legacy dashboard data (keep for backward compatibility)
        /// </summary>
        Task<DirectorDashboardDto?> GetDashboardAsync(string userId);

        // ==========================================
        // BAND ACCESS & ANALYTICS (EXISTING)
        // ==========================================

        /// <summary>
        /// Check if director can access a specific band
        /// </summary>
        Task<bool> CanAccessBandAsync(string userId, int bandId);

        /// <summary>
        /// Get band analytics for date range
        /// </summary>
        Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate);

        // ==========================================
        // STAFF MANAGEMENT (EXISTING)
        // ==========================================

        /// <summary>
        /// Add new staff member to band
        /// </summary>
        Task<BandStaffDto> AddStaffMemberAsync(string directorUserId, CreateBandStaffDto request);

        /// <summary>
        /// Check if user can manage a specific staff member
        /// </summary>
        Task<bool> CanManageStaffAsync(string userId, int staffId);

        /// <summary>
        /// Update existing staff member
        /// </summary>
        Task<BandStaffDto> UpdateStaffMemberAsync(int staffId, UpdateBandStaffDto request);

        /// <summary>
        /// Remove/deactivate staff member
        /// </summary>
        Task RemoveStaffMemberAsync(int staffId);

        /// <summary>
        /// Get all staff members with optional filtering
        /// </summary>
        Task<List<BandStaffDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy);

        // ==========================================
        // SCHOLARSHIP MANAGEMENT (EXISTING)
        // ==========================================

        /// <summary>
        /// Get scholarship overview with filtering
        /// </summary>
        Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters);

        /// <summary>
        /// Check if user can manage a specific scholarship offer
        /// </summary>
        Task<bool> CanManageScholarshipAsync(string userId, int offerId);

        /// <summary>
        /// Approve scholarship offer (detailed version - returns full DTO)
        /// </summary>
        Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes);

        /// <summary>
        /// Rescind/cancel scholarship offer
        /// </summary>
        Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason);

        // ==========================================
        // APPROVAL WORKFLOW (NEW - Dashboard Quick Actions)
        // ==========================================

        /// <summary>
        /// Get pending approvals for dashboard
        /// </summary>
        Task<List<PendingApprovalDto>> GetPendingApprovalsAsync(int bandId);

        /// <summary>
        /// Quick approve offer from dashboard (returns bool for simplicity)
        /// </summary>
        Task<bool> ApproveOfferAsync(int approvalId, string directorUserId, string? notes);

        /// <summary>
        /// Quick deny offer from dashboard
        /// </summary>
        Task<bool> DenyOfferAsync(int approvalId, string directorUserId, string reason);

        // ==========================================
        // STUDENTS & EVENTS (EXISTING)
        // ==========================================

        /// <summary>
        /// Get list of interested students
        /// </summary>
        Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters);

        /// <summary>
        /// Get band events with filtering
        /// </summary>
        Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters);
    }
}