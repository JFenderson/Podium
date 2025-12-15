using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.BandStaff;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces; // Updated to Core.Interfaces
using Podium.Infrastructure.Authorization;

namespace Podium.Application.Authorization
{
    /// <summary>
    /// Service for checking user permissions in business logic
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(
            IHttpContextAccessor httpContextAccessor,
            IUnitOfWork unitOfWork,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
        }

        public async Task<string?> GetCurrentUserIdAsync()
        {
            if (_httpContextAccessor.HttpContext?.User == null) return null;
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            return user?.Id;
        }

        public async Task<string?> GetCurrentUserRoleAsync()
        {
            if (_httpContextAccessor.HttpContext?.User == null) return null;
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (user == null) return null;
            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        public async Task<bool> HasRoleAsync(string role)
        {
            if (_httpContextAccessor.HttpContext?.User == null) return false;
            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (user == null) return false;
            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<bool> HasPermissionAsync(string permission)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return false;

            var bandStaff = await _unitOfWork.BandStaff.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (bandStaff == null) return false;

            return permission switch
            {
                Permissions.ViewStudents => bandStaff.CanViewStudents,
                Permissions.RateStudents => bandStaff.CanRateStudents,
                Permissions.SendOffers => bandStaff.CanSendOffers,
                Permissions.ManageEvents => bandStaff.CanManageEvents,
                Permissions.ManageStaff => bandStaff.CanManageStaff,
                _ => false
            };
        }

        public async Task<bool> IsStudentOwnerAsync(int studentId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return false;

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Student) return false;

            return await _unitOfWork.Students.GetQueryable()
                .AnyAsync(s => s.Id == studentId && s.ApplicationUserId == userId);
        }

        public async Task<bool> IsGuardianOfStudentAsync(int studentId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return false;

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Guardian) return false;

            var guardian = await _unitOfWork.Guardians.GetQueryable()
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);

            return guardian?.Students?.Any(s => s.Id == studentId) == true;
        }

        public async Task<bool> CanApproveScholarshipsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return false;

            var bandStaff = await _unitOfWork.BandStaff.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            return bandStaff?.Role == "Director";
        }

        public async Task<bool> CanSendOffersAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return false;

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Recruiter && role != Roles.Director) return false;

            var bandStaff = await _unitOfWork.BandStaff.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            return bandStaff?.CanSendOffers == true;
        }

        public async Task<BandStaffPermissionsDto?> GetBandStaffPermissionsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null) return null;

            var bandStaff = await _unitOfWork.BandStaff.GetQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (bandStaff == null) return null;

            return new BandStaffPermissionsDto
            {
                Role = bandStaff.Role,
                CanViewStudents = bandStaff.CanViewStudents,
                CanRateStudents = bandStaff.CanRateStudents,
                CanSendOffers = bandStaff.CanSendOffers,
                CanManageEvents = bandStaff.CanManageEvents,
                CanManageStaff = bandStaff.CanManageStaff
            };
        }
    }
}