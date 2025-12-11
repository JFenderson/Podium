using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Application.Services;
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
        private readonly IPermissionService _permissionService;
        private readonly Microsoft.AspNetCore.Authorization.IAuthorizationService _policyAuthService;
        private readonly IStudentService _studentService;

        public StudentsController(
            ApplicationDbContext context,
            IPermissionService permissionService,
            Microsoft.AspNetCore.Authorization.IAuthorizationService policyAuthService,
            IStudentService studentService) 
        {
            _context = context;
            _permissionService = permissionService;
            _policyAuthService = policyAuthService;
            _studentService = studentService;
        }

        /// <summary>
        /// Get all students - Only BandStaff with CanViewStudents permission
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDetailsDto>>> GetAllStudents()
        {
            var result = await _studentService.GetAccessibleStudentsAsync();
            return HandleResult(result);
        }

        /// <summary>
        /// Get student by ID - Students can view their own, Guardians can view linked students,
        /// BandStaff with CanViewStudents can view any
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDetailsDto>> GetStudent(int id)
        {
            var result = await _studentService.GetStudentDetailsAsync(id);
            return HandleResult(result);
        }

        /// <summary>
        /// Update student profile - Only the student themselves can update their profile
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            var result = await _studentService.UpdateStudentProfileAsync(id, dto);
            return HandleResult(result);
        }

        /// <summary>
        /// Get current student's profile - Uses custom permission service
        /// </summary>
        [HttpGet("me")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<StudentDetailsDto>> GetMyProfile()
        {
            // Reuse the "GetAccessible" logic which returns just the user for Students
            var result = await _studentService.GetAccessibleStudentsAsync();

            if (!result.IsSuccess) return HandleResult(result);

            var student = result.Data?.FirstOrDefault();
            if (student == null) return NotFound("Student profile not found");

            return Ok(student);
        }

        /// <summary>
        /// Submit interest in a specific Band
        /// </summary>
        [HttpPost("interest")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> ShowInterest([FromBody] InterestDto dto)
        {
            var result = await _studentService.ShowInterestAsync(dto.StudentId, dto.BandId);
            return HandleResult(result);
        }

        /// <summary>
        /// Rate a student (BandStaff only)
        /// </summary>
        [HttpPost("{id}/rate")]
        [Authorize(Policy = "CanRateStudents")]
        public async Task<IActionResult> RateStudent(int id, [FromBody] RatingDto dto)
        {
            var result = await _studentService.RateStudentAsync(id, dto);
            return HandleResult(result);
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

        // Helper to handle ServiceResult responses
        private ActionResult HandleResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess)
            {
                return result.Data == null ? NoContent() : Ok(result.Data);
            }

            return result.ResultType switch
            {
                ServiceResultType.NotFound => NotFound(result.ErrorMessage),
                ServiceResultType.Forbidden => Forbid(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };
        }
    }
}