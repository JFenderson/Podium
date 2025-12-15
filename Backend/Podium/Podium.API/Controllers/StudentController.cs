using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Application.Services;
using Podium.Core.Constants;
using Podium.Core.Interfaces;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionService _permissionService;
        private readonly IStudentService _studentService;

        public StudentsController(
            IUnitOfWork unitOfWork,
            IPermissionService permissionService,
            IStudentService studentService)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
            _studentService = studentService;
        }

        // ... [GetAllStudents, GetStudent, UpdateStudent, GetMyProfile, ShowInterest, RateStudent, SpecialAction] remain same ...
        // (They use _studentService which is already refactored)

        [HttpGet]
        public async Task<ActionResult<IEnumerable<StudentDetailsDto>>> GetAllStudents()
        {
            var result = await _studentService.GetAccessibleStudentsAsync();
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StudentDetailsDto>> GetStudent(int id)
        {
            var result = await _studentService.GetStudentDetailsAsync(id);
            return HandleResult(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStudent(int id, [FromBody] UpdateStudentDto dto)
        {
            var result = await _studentService.UpdateStudentProfileAsync(id, dto);
            return HandleResult(result);
        }

        [HttpGet("me")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<StudentDetailsDto>> GetMyProfile()
        {
            var result = await _studentService.GetAccessibleStudentsAsync();
            if (!result.IsSuccess) return HandleResult(result);
            var student = result.Data?.FirstOrDefault();
            if (student == null) return NotFound("Student profile not found");
            return Ok(student);
        }

        [HttpPost("interest")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> ShowInterest([FromBody] InterestDto dto)
        {
            var result = await _studentService.ShowInterestAsync(dto.StudentId, dto.BandId);
            return HandleResult(result);
        }

        [HttpPost("{id}/rate")]
        [Authorize(Policy = "CanRateStudents")]
        public async Task<IActionResult> RateStudent(int id, [FromBody] RatingDto dto)
        {
            var result = await _studentService.RateStudentAsync(id, dto);
            return HandleResult(result);
        }

        [HttpPost("{id}/special-action")]
        public async Task<IActionResult> SpecialAction(int id)
        {
            var canView = await _permissionService.HasPermissionAsync(Permissions.ViewStudents);
            var canRate = await _permissionService.HasPermissionAsync(Permissions.RateStudents);
            if (!canView || !canRate) return Forbid();
            return Ok();
        }

        /// <summary>
        /// Example checking if user owns resource
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var isOwner = await _permissionService.IsStudentOwnerAsync(id);
            var canManageStaff = await _permissionService.HasPermissionAsync(Permissions.ManageStaff);

            if (!isOwner && !canManageStaff) return Forbid();

            var student = await _unitOfWork.Students.GetByIdAsync(id);
            if (student == null) return NotFound();

            _unitOfWork.Students.Remove(student);
            await _unitOfWork.SaveChangesAsync();

            return NoContent();
        }

        private ActionResult HandleResult<T>(ServiceResult<T> result)
        {
            if (result.IsSuccess) return result.Data == null ? NoContent() : Ok(result.Data);
            return result.ResultType switch
            {
                ServiceResultType.NotFound => NotFound(result.ErrorMessage),
                ServiceResultType.Forbidden => Forbid(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };
        }
    }
}