using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.DTOs.Director;

namespace Podium.Core.Interfaces
{
    public interface IDirectorService
    {
        Task<DirectorDashboardDto?> GetDashboardAsync(string userId);
        Task<bool> CanAccessBandAsync(string userId, int bandId);
        Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate);
        Task<StaffMemberDto> AddStaffMemberAsync(string directorUserId, AddStaffRequest request);
        Task<bool> CanManageStaffAsync(string userId, int staffId);
        Task<StaffMemberDto> UpdateStaffMemberAsync(int staffId, UpdateStaffRequest request);
        Task RemoveStaffMemberAsync(int staffId);
        Task<List<StaffMemberDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy);
        Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters);
        Task<bool> CanManageScholarshipAsync(string userId, int offerId);
        Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes);
        Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason);
        Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters);
        Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters);
    }
}
