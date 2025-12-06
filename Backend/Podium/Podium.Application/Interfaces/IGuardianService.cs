using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Interfaces
{
    public interface IGuardianService
    {
        Task<List<LinkedStudentDto>> GetLinkedStudentsAsync(string guardianUserId);
        Task<bool> CanAccessStudentAsync(string guardianUserId, int studentId);
        Task<StudentActivityDto> GetStudentActivityAsync(int studentId, int daysBack);
        Task<StudentProfileDto> GetStudentProfileAsync(int studentId);
        Task<List<ContactRequestDto>> GetContactRequestsAsync(string guardianUserId, int? studentId, string? status);
        Task<bool> CanManageContactRequestAsync(string guardianUserId, int requestId);
        Task<ContactRequestDto> ApproveContactRequestAsync(int requestId, string guardianUserId, string? notes);
        Task<ContactRequestDto> DeclineContactRequestAsync(int requestId, string guardianUserId, string? reason);
        Task<List<GuardianScholarshipDto>> GetScholarshipsAsync(string guardianUserId, int? studentId, string? status);
        Task<bool> CanRespondToScholarshipAsync(string guardianUserId, int offerId);
        Task<GuardianScholarshipDto> RespondToScholarshipAsync(int offerId, string guardianUserId, string response, string? notes);
        Task<NotificationListDto> GetNotificationsAsync(string guardianUserId, NotificationFilterDto filters);
        Task<GuardianNotificationPreferencesDto> UpdateNotificationPreferencesAsync(string guardianUserId, UpdatePreferencesRequest request);
        Task<GuardianDashboardDto> GetDashboardAsync(string guardianUserId);
    }
}
