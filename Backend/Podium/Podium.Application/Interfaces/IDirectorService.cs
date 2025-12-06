using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandEvent;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    public interface IDirectorService
    {
        Task<DirectorDashboardDto?> GetDashboardAsync(string userId);
        Task<bool> CanAccessBandAsync(string userId, int bandId);
        Task<BandAnalyticsDto> GetBandAnalyticsAsync(int bandId, DateTime startDate, DateTime endDate);
        Task<BandStaffDto> AddStaffMemberAsync(string directorUserId, CreateBandStaffDto request);
        Task<bool> CanManageStaffAsync(string userId, int staffId);
        Task<BandStaffDto> UpdateStaffMemberAsync(int staffId, UpdateBandStaffDto request);
        Task RemoveStaffMemberAsync(int staffId);
        Task<List<BandStaffDto>> GetStaffMembersAsync(string userId, bool? isActive, string? sortBy);
        Task<ScholarshipOverviewDto> GetScholarshipsAsync(string userId, ScholarshipFilterDto filters);
        Task<bool> CanManageScholarshipAsync(string userId, int offerId);
        Task<ScholarshipOfferDto> ApproveScholarshipAsync(int offerId, string userId, string? notes);
        Task<ScholarshipOfferDto> RescindScholarshipAsync(int offerId, string userId, string reason);
        Task<List<InterestedStudentDto>> GetInterestedStudentsAsync(string userId, InterestedStudentFilterDto filters);
        Task<List<BandEventDto>> GetEventsAsync(string userId, EventFilterDto filters);
    }
}
