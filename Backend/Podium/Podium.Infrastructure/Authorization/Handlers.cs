using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Podium.Core.Entities;
using Podium.Infrastructure.Data;

namespace Podium.Infrastructure.Authorization
{
    /// <summary>
    /// Handler for role-based authorization
    /// </summary>
    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public RoleAuthorizationHandler(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            if (await _userManager.IsInRoleAsync(user, requirement.Role))
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Handler for BandStaff permission checks
    /// Verifies both role and specific permission flags
    /// </summary>
    public class BandStaffPermissionHandler : AuthorizationHandler<BandStaffPermissionRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public BandStaffPermissionHandler(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            BandStaffPermissionRequirement requirement)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            // Get BandStaff record with permissions - using string ApplicationUserId
            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == user.Id);

            if (bandStaff == null)
            {
                return;
            }

            // Check the specific permission
            bool hasPermission = requirement.Permission switch
            {
                Permissions.ViewStudents => bandStaff.CanViewStudents,
                Permissions.RateStudents => bandStaff.CanRateStudents,
                Permissions.SendOffers => bandStaff.CanSendOffers,
                Permissions.ManageEvents => bandStaff.CanManageEvents,
                Permissions.ManageStaff => bandStaff.CanManageStaff,
                _ => false
            };

            if (hasPermission)
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Handler for self-access authorization (users accessing their own resources)
    /// </summary>
    public class SelfAccessHandler : AuthorizationHandler<SelfAccessRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public SelfAccessHandler(
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            SelfAccessRequirement requirement)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            // Try to get resource ID from route
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request.RouteValues.TryGetValue("id", out var routeId) == true)
            {
                var resourceId = routeId?.ToString();
                if (resourceId == user.Id) // String comparison
                {
                    context.Succeed(requirement);
                }
            }
        }
    }

    /// <summary>
    /// Handler for guardian accessing their linked students
    /// </summary>
    public class GuardianStudentAccessHandler : AuthorizationHandler<GuardianStudentAccessRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public GuardianStudentAccessHandler(
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            GuardianStudentAccessRequirement requirement)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            // Verify user is a guardian
            if (!await _userManager.IsInRoleAsync(user, Roles.Guardian))
            {
                return;
            }

            // Get the student ID from route
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Request.RouteValues.TryGetValue("studentId", out var studentIdObj) != true)
            {
                return;
            }

            if (!int.TryParse(studentIdObj?.ToString(), out int studentId))
            {
                return;
            }

            // Check if guardian is linked to this student - using string ApplicationUserId
            var guardian = await _context.Guardians
                .Include(g => g.Students)
                .FirstOrDefaultAsync(g => g.ApplicationUserId == user.Id);

            if (guardian?.Students?.Any(s => s.StudentId == studentId) == true)
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Handler for scholarship approval (Directors only)
    /// </summary>
    public class ScholarshipApprovalHandler : AuthorizationHandler<ScholarshipApprovalRequirement>
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ScholarshipApprovalHandler(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ScholarshipApprovalRequirement requirement)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            // Check if user is a Director - using string ApplicationUserId
            var bandStaff = await _context.BandStaff
                .AsNoTracking()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == user.Id);

            if (bandStaff?.Role == "Director")
            {
                context.Succeed(requirement);
            }
        }
    }

    /// <summary>
    /// Resource-based authorization handler for Student entities
    /// </summary>
    public class StudentResourceAuthorizationHandler :
        AuthorizationHandler<ResourceAccessRequirement, int>
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public StudentResourceAuthorizationHandler(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            ResourceAccessRequirement requirement,
            int studentId)
        {
            if (context.User == null || !context.User.Identity!.IsAuthenticated)
            {
                return;
            }

            var user = await _userManager.GetUserAsync(context.User);
            if (user == null)
            {
                return;
            }

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            switch (requirement.Operation)
            {
                case Operations.Read:
                    // Students can read their own data - using string ApplicationUserId
                    if (role == Roles.Student)
                    {
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.ApplicationUserId == user.Id);
                        if (student != null)
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }

                    // Guardians can read linked students - using string ApplicationUserId
                    if (role == Roles.Guardian)
                    {
                        var guardian = await _context.Guardians
                            .Include(g => g.Students)
                            .FirstOrDefaultAsync(g => g.ApplicationUserId == user.Id);

                        if (guardian?.Students?.Any(s => s.StudentId == studentId) == true)
                        {
                            context.Succeed(requirement);
                            return;
                        }
                    }

                    // BandStaff with ViewStudents permission - using string ApplicationUserId
                    if (role == Roles.Recruiter || role == Roles.Director)
                    {
                        var bandStaff = await _context.BandStaff
                            .FirstOrDefaultAsync(bs => bs.ApplicationUserId == user.Id);

                        if (bandStaff?.CanViewStudents == true)
                        {
                            context.Succeed(requirement);
                        }
                    }
                    break;

                case Operations.Update:
                    // Students can only update their own profile - using string ApplicationUserId
                    if (role == Roles.Student)
                    {
                        var student = await _context.Students
                            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.ApplicationUserId == user.Id);
                        if (student != null)
                        {
                            context.Succeed(requirement);
                        }
                    }
                    break;

                case Operations.Delete:
                    // Only Directors can delete student records - using string ApplicationUserId
                    if (role == Roles.Director)
                    {
                        var bandStaff = await _context.BandStaff
                            .FirstOrDefaultAsync(bs => bs.ApplicationUserId == user.Id);

                        if (bandStaff?.CanManageStaff == true)
                        {
                            context.Succeed(requirement);
                        }
                    }
                    break;
            }
        }
    }
}