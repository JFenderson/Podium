using Podium.Application.DTOs.Guardian;
using Podium.Application.Services;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Podium.Application.Interfaces
{
    public interface IGuardianService
    {
        Task<List<LinkedStudentDto>> GetLinkedStudentsAsync(string guardianUserId);
        Task<bool> CanAccessStudentAsync(string guardianUserId, int studentId);
        Task<LinkedStudentActivityReportDto> GetStudentActivityAsync(int studentId, int daysBack);
        Task<LinkedStudentProfileViewDto> GetStudentProfileAsync(int studentId);
        Task<List<ContactRequestDto>> GetContactRequestsAsync(string guardianUserId, int? studentId, string? status);
        Task<bool> CanManageContactRequestAsync(string guardianUserId, int requestId);
        Task<ContactRequestDto> ApproveContactRequestAsync(int requestId, string guardianUserId, string? notes);
        Task<ContactRequestDto> DeclineContactRequestAsync(int requestId, string guardianUserId, string? reason);
        Task<List<GuardianScholarshipDto>> GetScholarshipsAsync(string guardianUserId, int? studentId, string? status);
        Task<bool> CanRespondToScholarshipAsync(string guardianUserId, int offerId);
        Task<GuardianScholarshipDto> RespondToScholarshipAsync(int offerId, string guardianUserId, string response, string? notes);
        Task<List<string>> GetGuardianUserIdsForStudentAsync(int studentId);

        Task<ServiceResult<NotificationListDto>> GetNotificationsAsync(string guardianUserId, NotificationFilterDto filters);

        Task<ServiceResult<GuardianDashboardDto>> GetDashboardAsync();
        Task<ServiceResult<GuardianNotificationPreferencesDto>> UpdateNotificationPreferencesAsync(UpdatePreferencesRequest request);
    }
}
