using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.Interfaces;
using Podium.Core.Constants; // For Roles constant
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using System.Security.Claims;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BandStaffController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionService _permissionService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBandStaffService _bandStaffService;
        private readonly ILogger<BandStaffController> _logger;

        public BandStaffController(
            IUnitOfWork unitOfWork,
            IPermissionService permissionService,
            UserManager<ApplicationUser> userManager,
            IBandStaffService bandStaffService,
            ILogger<BandStaffController> logger)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
            _userManager = userManager;
            _bandStaffService = bandStaffService;
            _logger = logger;
        }


        /// <summary>
        /// Get complete band staff dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(BandStaffDashboardDto), 200)]
        public async Task<ActionResult<BandStaffDashboardDto>> GetDashboard(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? instrument,
            [FromQuery] string? contactStatus)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var filters = new BandStaffDashboardFiltersDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Instrument = instrument,
                    ContactStatus = contactStatus
                };

                var dashboard = await _bandStaffService.GetDashboardAsync(userId, filters);
                return Ok(dashboard);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized dashboard access attempt");
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading band staff dashboard");
                return StatusCode(500, "An error occurred while loading the dashboard");
            }
        }

        /// <summary>
        /// Get personal metrics only
        /// </summary>
        [HttpGet("metrics")]
        [ProducesResponseType(typeof(BandStaffPersonalMetricsDto), 200)]
        public async Task<ActionResult<BandStaffPersonalMetricsDto>> GetMetrics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var metrics = await _bandStaffService.GetPersonalMetricsAsync(userId, start, end);
                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading metrics");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Get my students
        /// </summary>
        [HttpGet("my-students")]
        [ProducesResponseType(typeof(List<MyStudentDto>), 200)]
        public async Task<ActionResult<List<MyStudentDto>>> GetMyStudents([FromQuery] string? status)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var students = await _bandStaffService.GetMyStudentsAsync(userId, status);
                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading my students");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Get my performance
        /// </summary>
        [HttpGet("performance")]
        [ProducesResponseType(typeof(BandStaffPerformanceDto), 200)]
        public async Task<ActionResult<BandStaffPerformanceDto>> GetPerformance(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                var performance = await _bandStaffService.GetMyPerformanceAsync(userId, start, end);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading performance");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Get my recent activity
        /// </summary>
        [HttpGet("activity")]
        [ProducesResponseType(typeof(List<MyActivityDto>), 200)]
        public async Task<ActionResult<List<MyActivityDto>>> GetActivity([FromQuery] int limit = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var activity = await _bandStaffService.GetMyActivityAsync(userId, limit);
                return Ok(activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading activity");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Get my pending tasks
        /// </summary>
        [HttpGet("tasks")]
        [ProducesResponseType(typeof(List<MyPendingTaskDto>), 200)]
        public async Task<ActionResult<List<MyPendingTaskDto>>> GetTasks()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var tasks = await _bandStaffService.GetMyPendingTasksAsync(userId);
                return Ok(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading tasks");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Get quick stats
        /// </summary>
        [HttpGet("quick-stats")]
        [ProducesResponseType(typeof(QuickStatsDto), 200)]
        public async Task<ActionResult<QuickStatsDto>> GetQuickStats()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var stats = await _bandStaffService.GetQuickStatsAsync(userId);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quick stats");
                return StatusCode(500, "An error occurred");
            }
        }

        /// <summary>
        /// Search students
        /// </summary>
        [HttpGet("search-students")]
        [ProducesResponseType(typeof(StaffStudentSearchDto), 200)]
        public async Task<ActionResult<StaffStudentSearchDto>> SearchStudents(
            [FromQuery] string? searchTerm,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var results = await _bandStaffService.SearchStudentsAsync(userId, searchTerm, page, pageSize);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching students");
                return StatusCode(500, "An error occurred");
            }
        }

        // ==========================================
        // EXISTING / ADMIN ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get all band staff - Only Directors with ManageStaff permission
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<BandStaffDto>>> GetAllStaff()
        {
            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .Select(bs => new BandStaffDto
                {
                    BandStaffId = bs.Id,
                    BandId = bs.BandId,
                    ApplicationUserId = bs.ApplicationUserId,
                    FirstName = bs.FirstName,
                    LastName = bs.LastName,
                    Email = bs.ApplicationUser.Email,
                    Role = bs.Role,
                    Title = bs.Title,
                    IsActive = bs.IsActive,
                    CanViewStudents = bs.CanViewStudents,
                    CanRateStudents = bs.CanRateStudents,
                    CanSendOffers = bs.CanSendOffers,
                    CanManageEvents = bs.CanManageEvents,
                    CanManageStaff = bs.CanManageStaff,
                    CanContact = bs.CanContact,
                    CanMakeOffers = bs.CanMakeOffers,
                    CanViewFinancials = bs.CanViewFinancials
                })
                .ToListAsync();

            return Ok(staff);
        }

        /// <summary>
        /// Get current staff member's information
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "BandStaffOnly")]
        public async Task<ActionResult<BandStaffDto>> GetMyInfo()
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (userId == null) return Unauthorized();

            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (staff == null) return NotFound("Staff profile not found");

            return Ok(MapToDto(staff));
        }

        /// <summary>
        /// Create new staff member - Only Directors with ManageStaff permission
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<BandStaffDto>> CreateStaff([FromBody] CreateBandStaffDto dto)
        {
            var existingUser = await _userManager.FindByIdAsync(dto.ApplicationUserId);
            if (existingUser == null) return BadRequest("User not found");

            var existingStaff = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == dto.ApplicationUserId && bs.BandId == dto.BandId);

            if (existingStaff != null) return BadRequest("Staff profile already exists for this user in this band");

            var staff = new BandStaff
            {
                BandId = dto.BandId,
                ApplicationUserId = dto.ApplicationUserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                IsActive = true,
                CreatedBy = User.FindFirstValue(ClaimTypes.Email) ?? "System",

                // Map Permissions
                CanViewStudents = dto.CanViewStudents,
                CanRateStudents = dto.CanRateStudents,
                CanSendOffers = dto.CanSendOffers,
                CanManageEvents = dto.CanManageEvents,
                CanManageStaff = dto.CanManageStaff,
                CanContact = dto.CanContact,
                CanMakeOffers = dto.CanMakeOffers,
                CanViewFinancials = dto.CanViewFinancials
            };

            await _unitOfWork.BandStaff.AddAsync(staff);
            await _unitOfWork.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMyInfo), MapToDto(staff));
        }

        /// <summary>
        /// Update staff permissions - Only Directors
        /// </summary>
        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] BandStaffPermissionsDto dto)
        {
            var staff = await _unitOfWork.BandStaff.GetByIdAsync(id);
            if (staff == null) return NotFound();

            var currentUserId = await _permissionService.GetCurrentUserIdAsync();
            if (staff.ApplicationUserId == currentUserId && !dto.CanManageStaff)
            {
                return BadRequest("You cannot remove your own ManageStaff permission");
            }

            staff.CanViewStudents = dto.CanViewStudents;
            staff.CanRateStudents = dto.CanRateStudents;
            staff.CanSendOffers = dto.CanSendOffers;
            staff.CanManageEvents = dto.CanManageEvents;
            staff.CanManageStaff = dto.CanManageStaff;
            // Note: Add other permissions here if they exist in BandStaffPermissionsDto

            _unitOfWork.BandStaff.Update(staff);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Permissions updated successfully" });
        }

        // ==========================================
        // NEW / REQUESTED ENDPOINTS
        // ==========================================

        /// <summary>
        /// Get a single staff member by ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<BandStaffDto>> GetStaffMember(int id)
        {
            var staffMember = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .FirstOrDefaultAsync(bs => bs.Id == id);

            if (staffMember == null) return NotFound("Staff member not found.");

            return Ok(MapToDto(staffMember));
        }

        /// <summary>
        /// Invite a new staff member via email
        /// </summary>
        [HttpPost("invite")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> InviteStaff([FromBody] InviteStaffDto inviteDto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // 1. Check if user exists
            var existingUser = await _userManager.FindByEmailAsync(inviteDto.Email);
            if (existingUser == null)
            {
                return BadRequest("User does not exist. Please ask them to register for an account first.");
            }

            // 2. Check if already staff for this band
            var existingStaff = await _unitOfWork.BandStaff.GetQueryable()
                .FirstOrDefaultAsync(bs => bs.BandId == inviteDto.BandId && bs.ApplicationUserId == existingUser.Id);

            if (existingStaff != null)
            {
                if (!existingStaff.IsActive)
                {
                    // Reactivate
                    existingStaff.IsActive = true;
                    existingStaff.Role = inviteDto.Role;
                    existingStaff.Title = inviteDto.Title;
                    existingStaff.DeactivatedDate = null;
                    _unitOfWork.BandStaff.Update(existingStaff);
                    await _unitOfWork.SaveChangesAsync();
                    return Ok(new { Message = "Staff member reactivated." });
                }
                return BadRequest("User is already a staff member for this band.");
            }

            // 3. Create new BandStaff
            var newStaff = new BandStaff
            {
                BandId = inviteDto.BandId,
                ApplicationUserId = existingUser.Id,
                FirstName = inviteDto.FirstName != "" ? inviteDto.FirstName : existingUser.FirstName,
                LastName = inviteDto.LastName != "" ? inviteDto.LastName : existingUser.LastName,
                Role = inviteDto.Role,
                Title = inviteDto.Title ?? "Staff",
                IsActive = true,
                CreatedBy = User.FindFirstValue(ClaimTypes.Email) ?? "System",

                // Default permissions from Invite DTO
                CanContact = inviteDto.CanContact,
                CanViewStudents = inviteDto.CanViewStudents,
                CanMakeOffers = inviteDto.CanMakeOffers,
                CanViewFinancials = inviteDto.CanViewFinancials,
                CanRateStudents = inviteDto.CanRateStudents,
                CanSendOffers = inviteDto.CanSendOffers,
                CanManageEvents = inviteDto.CanManageEvents,
                CanManageStaff = inviteDto.CanManageStaff
            };

            await _unitOfWork.BandStaff.AddAsync(newStaff);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Staff member invited successfully", StaffId = newStaff.Id });
        }

        /// <summary>
        /// Update staff details (Role, Title, Permissions)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UpdateStaffMember(int id, [FromBody] UpdateBandStaffDto updateDto)
        {
            var staff = await _unitOfWork.BandStaff.GetByIdAsync(id);
            if (staff == null) return NotFound();

            // Basic Info
            staff.Role = updateDto.Role;
            staff.IsActive = updateDto.IsActive;

            // Map Permissions (Flat properties from DTO)
            staff.CanContact = updateDto.CanContact;
            staff.CanMakeOffers = updateDto.CanMakeOffers;
            staff.CanViewFinancials = updateDto.CanViewFinancials;
            staff.CanViewStudents = updateDto.CanViewStudents;
            staff.CanRateStudents = updateDto.CanRateStudents;
            staff.CanSendOffers = updateDto.CanSendOffers;
            staff.CanManageEvents = updateDto.CanManageEvents;
            staff.CanManageStaff = updateDto.CanManageStaff;

            staff.ModifiedBy = User.FindFirstValue(ClaimTypes.Email);

            _unitOfWork.BandStaff.Update(staff);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Staff member updated successfully" });
        }

        /// <summary>
        /// Soft delete / Deactivate staff
        /// </summary>
        [HttpPost("{id}/deactivate")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> DeactivateStaff(int id)
        {
            var staff = await _unitOfWork.BandStaff.GetByIdAsync(id);
            if (staff == null) return NotFound();

            staff.IsActive = false;
            staff.DeactivatedDate = DateTime.UtcNow;
            staff.ModifiedBy = User.FindFirstValue(ClaimTypes.Email);

            _unitOfWork.BandStaff.Update(staff);
            await _unitOfWork.SaveChangesAsync();

            return Ok(new { Message = "Staff member deactivated." });
        }

        /// <summary>
        /// Search for staff members
        /// </summary>
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<BandStaffSummaryDto>>> SearchStaff([FromQuery] int bandId, [FromQuery] string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return BadRequest("Query is required");

            var staff = await _unitOfWork.BandStaff.GetQueryable()
                .Include(bs => bs.ApplicationUser)
                .Where(bs => bs.BandId == bandId &&
                            (bs.FirstName.Contains(query) ||
                             bs.LastName.Contains(query) ||
                             bs.ApplicationUser.Email.Contains(query)))
                .Select(bs => new BandStaffSummaryDto
                {
                    Id = bs.Id,
                    UserId = bs.ApplicationUserId,
                    FullName = $"{bs.FirstName} {bs.LastName}",
                    Email = bs.ApplicationUser.Email,
                    Role = bs.Role,
                    Title = bs.Title,
                    IsActive = bs.IsActive
                })
                .ToListAsync();

            return Ok(staff);
        }

        // ==========================================
        // HELPERS
        // ==========================================

        private static BandStaffDto MapToDto(BandStaff staff)
        {
            return new BandStaffDto
            {
                BandStaffId = staff.Id,
                BandId = staff.BandId,
                ApplicationUserId = staff.ApplicationUserId,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Email = staff.ApplicationUser?.Email,
                Role = staff.Role,
                Title = staff.Title,
                IsActive = staff.IsActive,
                JoinedDate = staff.JoinedDate,
                LastActivityDate = staff.LastActivityDate,
                DeactivatedDate = staff.DeactivatedDate,

                // Permissions
                CanViewStudents = staff.CanViewStudents,
                CanRateStudents = staff.CanRateStudents,
                CanSendOffers = staff.CanSendOffers,
                CanManageEvents = staff.CanManageEvents,
                CanManageStaff = staff.CanManageStaff,
                CanContact = staff.CanContact,
                CanMakeOffers = staff.CanMakeOffers,
                CanViewFinancials = staff.CanViewFinancials,

                // Metrics
                TotalContactsInitiated = staff.TotalContactsInitiated,
                TotalOffersCreated = staff.TotalOffersCreated,
                SuccessfulPlacements = staff.SuccessfulPlacements
            };
        }
    }
}