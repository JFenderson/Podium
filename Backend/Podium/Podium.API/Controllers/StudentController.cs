using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Core.Constants;
using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication for all endpoints
    public class StudentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IPermissionService _permissionService; // ✅ Renamed
        private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _policyAuthService; // ✅ Full namespace

        public StudentsController(
            ApplicationDbContext context,
            IPermissionService permissionService, // ✅ Changed parameter
            Microsoft.AspNetCore.Authorization.IAuthorizationService policyAuthService)
        {
            _context = context;
            _permissionService = permissionService;
            _policyAuthService = policyAuthService;
        }

        /// <summary>
        /// Get all students - Only BandStaff with CanViewStudents permission
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "CanViewStudents")]
        public async Task<ActionResult<IEnumerable<StudentDto>>> GetAllStudents()
        {
            var students = await _context.Students
                .Select(s => new StudentDto
                {
                    StudentId = s.StudentId,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    Email = s.Email,
                    Instrument = s.Instrument
                })
                .ToListAsync();

            return Ok(students);
        }

        /// <summary>
        /// Get student by ID - Students can view their own, Guardians can view linked students,
        /// BandStaff with CanViewStudents can view any
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDto>> GetStudent(int id)
        {
            // Resource-based authorization
            var authResult = await _policyAuthService.AuthorizeAsync(
                User,
                id,
                new ResourceAccessRequirement(Operations.ReadOperation));

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.StudentId == id);

            if (student == null)
            {
                return NotFound();
            }

            return Ok(new StudentDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                Instrument = student.Instrument
            });
        }

        /// <summary>
        /// Update student profile - Only the student themselves can update their profile
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            // Check if user can update this student
            var authResult = await _policyAuthService.AuthorizeAsync(
                User,
                id,
                new ResourceAccessRequirement(Operations.UpdateOperation));

            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            // Update student properties
            student.FirstName = dto.FirstName;
            student.LastName = dto.LastName;
            student.Bio = dto.Bio;
            student.Instrument = dto.Instrument;

            await _context.SaveChangesAsync();

            return NoContent();
        }


        /// <summary>
        /// Get current student's profile - Uses custom permission service
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<StudentDto>> GetMyProfile()
        {
            var userId = await _permissionService.GetCurrentUserIdAsync();
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId);

            if (student == null)
            {
                return NotFound("Student profile not found");
            }

            return Ok(new StudentDto
            {
                StudentId = student.StudentId,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                Instrument = student.Instrument
            });
        }

        /// <summary>
        /// Rate a student - Only BandStaff with CanRateStudents permission
        /// </summary>
        [HttpPost("{id}/rate")]
        [Authorize(Policy = "CanRateStudents")]
        public async Task<IActionResult> RateStudent(int id, [FromBody] RatingDto dto)
        {
            // Verify student exists
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var userId = await _permissionService.GetCurrentUserIdAsync(); // ✅ Using IPermissionService
            if (userId == null)
            {
                return Unauthorized();
            }

            // Create rating (you'll need to create this entity)
            // var rating = new StudentRating { ... };

            return Ok(new { Message = "Rating submitted successfully" });
        }

        /// <summary>
        /// Example using permission check in controller
        /// </summary>
        [HttpPost("{id}/special-action")]
        public async Task<IActionResult> SpecialAction(int id)
        {
            // Check multiple permissions
            var canView = await _permissionService.HasPermissionAsync(Permissions.ViewStudents);
            var canRate = await _permissionService.HasPermissionAsync(Permissions.RateStudents);

            if (!canView || !canRate)
            {
                return Forbid();
            }

            // Perform action...
            return Ok();
        }

        /// <summary>
        /// Example checking if user owns resource
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            // Only student can delete their own profile OR admin
            var isOwner = await _permissionService.IsStudentOwnerAsync(id);
            var canManageStaff = await _permissionService.HasPermissionAsync(Permissions.ManageStaff);

            if (!isOwner && !canManageStaff)
            {
                return Forbid();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            _context.Students.Remove(student);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}