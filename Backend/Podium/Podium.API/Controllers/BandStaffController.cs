using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Student;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Rating;
using Podium.Core.Entities;
using Podium.Application.Authorization;

namespace Podium.API.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BandStaffController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService;

        public BandStaffController(
            ApplicationDbContext context,
            IPermissionService permissionService)
        {
            _context = context;
            _permissionService = permissionService;
        }

        /// <summary>
        /// Get all band staff - Only Directors with ManageStaff permission
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<IEnumerable<BandStaffDto>>> GetAllStaff()
        {
            var staff = await _context.BandStaff
                .Include(bs => bs.ApplicationUser)
                .Select(bs => new BandStaffDto
                {
                    BandStaffId = bs.Id,
                    ApplicationUserId = bs.ApplicationUserId,
                    FirstName = bs.FirstName,
                    LastName = bs.LastName,
                    Role = bs.Role,
                    CanViewStudents = bs.CanViewStudents,
                    CanRateStudents = bs.CanRateStudents,
                    CanSendOffers = bs.CanSendOffers,
                    CanManageEvents = bs.CanManageEvents,
                    CanManageStaff = bs.CanManageStaff
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
            if (userId == null)
            {
                return Unauthorized();
            }

            var staff = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == userId);

            if (staff == null)
            {
                return NotFound("Staff profile not found");
            }

            return Ok(new BandStaffDto
            {
                BandStaffId = staff.Id,
                ApplicationUserId = staff.ApplicationUserId,
                FirstName = staff.FirstName,
                LastName = staff.LastName,
                Role = staff.Role,
                CanViewStudents = staff.CanViewStudents,
                CanRateStudents = staff.CanRateStudents,
                CanSendOffers = staff.CanSendOffers,
                CanManageEvents = staff.CanManageEvents,
                CanManageStaff = staff.CanManageStaff
            });
        }

        /// <summary>
        /// Update staff permissions - Only Directors with ManageStaff permission
        /// </summary>
        [HttpPut("{id}/permissions")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> UpdatePermissions(int id, [FromBody] BandStaffPermissionsDto dto)
        {
            var staff = await _context.BandStaff.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            // Prevent Directors from removing their own ManageStaff permission
            var currentUserId = await _permissionService.GetCurrentUserIdAsync();
            if (staff.ApplicationUserId == currentUserId && !dto.CanManageStaff)
            {
                return BadRequest("You cannot remove your own ManageStaff permission");
            }

            // Update permissions
            staff.CanViewStudents = dto.CanViewStudents;
            staff.CanRateStudents = dto.CanRateStudents;
            staff.CanSendOffers = dto.CanSendOffers;
            staff.CanManageEvents = dto.CanManageEvents;
            staff.CanManageStaff = dto.CanManageStaff;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Permissions updated successfully" });
        }

        /// <summary>
        /// Promote Recruiter to Director - Only Directors with ManageStaff permission
        /// </summary>
        [HttpPost("{id}/promote")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> PromoteToDirector(int id)
        {
            var staff = await _context.BandStaff.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            if (staff.Role == "Director")
            {
                return BadRequest("Staff member is already a Director");
            }

            staff.Role = "Director";
            // Optionally grant additional permissions when promoting
            staff.CanManageStaff = true;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Staff member promoted to Director" });
        }

        /// <summary>
        /// Create new staff member - Only Directors with ManageStaff permission
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "AdminAccess")]
        public async Task<ActionResult<BandStaffDto>> CreateStaff([FromBody] CreateBandStaffDto dto)
        {
            // Check if user already exists
            var existingUser = await _context.Users.FindAsync(dto.ApplicationUserId);
            if (existingUser == null)
            {
                return BadRequest("User not found");
            }

            // Check if staff profile already exists
            var existingStaff = await _context.BandStaff
                .FirstOrDefaultAsync(bs => bs.ApplicationUserId == dto.ApplicationUserId);
            if (existingStaff != null)
            {
                return BadRequest("Staff profile already exists for this user");
            }

            var staff = new BandStaff
            {
                ApplicationUserId = dto.ApplicationUserId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Role = dto.Role,
                CanViewStudents = dto.CanViewStudents,
                CanRateStudents = dto.CanRateStudents,
                CanSendOffers = dto.CanSendOffers,
                CanManageEvents = dto.CanManageEvents,
                CanManageStaff = dto.CanManageStaff
            };

            _context.BandStaff.Add(staff);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMyInfo),
                new BandStaffDto
                {
                    BandStaffId = staff.Id,
                    ApplicationUserId = staff.ApplicationUserId,
                    FirstName = staff.FirstName,
                    LastName = staff.LastName,
                    Role = staff.Role,
                    CanViewStudents = staff.CanViewStudents,
                    CanRateStudents = staff.CanRateStudents,
                    CanSendOffers = staff.CanSendOffers,
                    CanManageEvents = staff.CanManageEvents,
                    CanManageStaff = staff.CanManageStaff
                });
        }

        /// <summary>
        /// Get my permissions - Any authenticated BandStaff can check their own permissions
        /// </summary>
        [HttpGet("my-permissions")]
        [Authorize(Policy = "BandStaffOnly")]
        public async Task<ActionResult<BandStaffPermissionsDto>> GetMyPermissions()
        {
            var permissions = await _permissionService.GetBandStaffPermissionsAsync();
            if (permissions == null)
            {
                return NotFound("Staff profile not found");
            }

            return Ok(permissions);
        }

        /// <summary>
        /// Remove staff member - Only Directors with ManageStaff permission
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "AdminAccess")]
        public async Task<IActionResult> RemoveStaff(int id)
        {
            var staff = await _context.BandStaff.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            // Prevent Directors from removing themselves
            var currentUserId = await _permissionService.GetCurrentUserIdAsync();
            if (staff.ApplicationUserId == currentUserId)
            {
                return BadRequest("You cannot remove yourself");
            }

            _context.BandStaff.Remove(staff);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}