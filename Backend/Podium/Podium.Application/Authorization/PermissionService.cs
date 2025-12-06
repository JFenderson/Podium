using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.BandStaff;
using Podium.Core.Entities;
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;

namespace Podium.Application.Authorization
{
    /// <summary>
    /// Service for checking user permissions in business logic
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PermissionService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Get the current authenticated user's ID (string/GUID)
        /// </summary>
        public async Task<string?> GetCurrentUserIdAsync()
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return null;
            }

            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            return user?.Id;
        }

        /// <summary>
        /// Get the current authenticated user's primary role
        /// </summary>
        public async Task<string?> GetCurrentUserRoleAsync()
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return null;
            }

            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        /// <summary>
        /// Check if current user has a specific role
        /// </summary>
        public async Task<bool> HasRoleAsync(string role)
        {
            if (_httpContextAccessor.HttpContext?.User == null)
            {
                return false;
            }

            var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
            if (user == null) return false;

            return await _userManager.IsInRoleAsync(user, role);
        }

        /// <summary>
        /// Check if current user has a specific BandStaff permission
        /// </summary>
        public async Task<bool> HasPermissionAsync(string permission)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }

            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (bandStaff == null)
            {
                return false;
            }

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

        /// <summary>
        /// Check if current user is the owner of a specific student profile
        /// </summary>
        public async Task<bool> IsStudentOwnerAsync(int studentId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Student)
            {
                return false;
            }

            return await _context.Students
                .AnyAsync(s => s.StudentId == studentId && s.ApplicationUserId == userId);
        }

        /// <summary>
        /// Check if current user is a guardian of a specific student
        /// </summary>
        public async Task<bool> IsGuardianOfStudentAsync(int studentId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Guardian)
            {
                return false;
            }

            var guardian = await _context.Guardians
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.ApplicationUserId == userId);

            return guardian?.Students?.Any(s => s.StudentId == studentId) == true;
        }

        /// <summary>
        /// Check if current user can approve scholarships (Directors only)
        /// </summary>
        public async Task<bool> CanApproveScholarshipsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }

            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            return bandStaff?.Role == "Director";
        }

        /// <summary>
        /// Check if current user can send offers (Recruiters/Directors with permission)
        /// </summary>
        public async Task<bool> CanSendOffersAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return false;
            }

            var role = await GetCurrentUserRoleAsync();
            if (role != Roles.Recruiter && role != Roles.Director)
            {
                return false;
            }

            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            return bandStaff?.CanSendOffers == true;
        }

        /// <summary>
        /// Get all permissions for the current BandStaff user
        /// </summary>
        public async Task<BandStaffPermissionsDto?> GetBandStaffPermissionsAsync()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
            {
                return null;
            }

            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (bandStaff == null)
            {
                return null;
            }

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