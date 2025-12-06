using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.DTOs;
using Podium.Application.DTOs.BandStaff;

namespace Podium.Application.Authorization
{
    public interface IPermissionService
    {
        Task<string?> GetCurrentUserIdAsync();
        Task<string?> GetCurrentUserRoleAsync();
        Task<bool> HasRoleAsync(string role);
        Task<bool> HasPermissionAsync(string permission);
        Task<bool> IsStudentOwnerAsync(int studentId);
        Task<bool> IsGuardianOfStudentAsync(int studentId);
        Task<bool> CanApproveScholarshipsAsync();
        Task<bool> CanSendOffersAsync();
        Task<UpdateBandStaffDto?> GetBandStaffPermissionsAsync();
    }
}
