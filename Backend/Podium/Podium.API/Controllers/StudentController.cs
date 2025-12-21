using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.DTOs.Video; // Required for CreateVideoRequest
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
        private readonly IVideoService _videoService;
        private readonly IVideoStorageService _storageService;

        public StudentsController(
            IUnitOfWork unitOfWork,
            IPermissionService permissionService,
            IStudentService studentService,
            IVideoService videoService,
            IVideoStorageService storageService)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
            _studentService = studentService;
            _videoService = videoService;
            _storageService = storageService;
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
            var result = await _studentService.GetAccessibleStudentsAsync(1, 1);
            if (!result.IsSuccess) return HandleResult(result);
            var student = result.Data?.Items.FirstOrDefault();
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


        /// <summary>
        /// Matches frontend: searchStudents(searchTerm)
        /// Endpoint: GET api/Students/search?search=term
        /// </summary>
        [HttpGet("search")]
        [AllowAnonymous] // Or [Authorize] depending on if public search is allowed
public async Task<ActionResult<IEnumerable<StudentSummaryDto>>> SearchStudents([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return Ok(new List<StudentSummaryDto>());

            var query = _unitOfWork.Students.GetQueryable()
                .Include(s => s.Videos)         // Eager load for Thumbnail access
                .Include(s => s.StudentRatings) // Eager load for Rating calc
                .Where(s => !s.IsDeleted &&
                           (s.FirstName.Contains(search) ||
                            s.LastName.Contains(search) ||
                            s.PrimaryInstrument.Contains(search) ||
                            s.PrimaryInstrument.Contains(search)));

            var students = await query
                .Take(20)
                .Select(s => new StudentSummaryDto
                {
                    StudentId = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    GraduationYear = s.GraduationYear,
                    PrimaryInstrument = s.PrimaryInstrument ?? s.PrimaryInstrument,
                    HighSchool = s.HighSchool,

                    // Logic: Try to get the Primary video thumbnail, otherwise grab the first available one
                    VideoThumbnailUrl = s.Videos
                        .Where(v => !v.IsDeleted && v.ThumbnailUrl != null)
                        .OrderByDescending(v => v.IsPrimary) // true comes first
                        .Select(v => v.ThumbnailUrl)
                        .FirstOrDefault(),

                    // Calculate Ratings
                    AverageRating = s.StudentRatings.Any() ? s.StudentRatings.Average(r => r.Rating) : (double?)null,
                    RatingCount = s.StudentRatings.Count()
                })
                .ToListAsync();

            return Ok(students);
        }

        /// <summary>
        /// Matches frontend: getStudentRatings(studentId)
        /// Endpoint: GET api/Students/{id}/ratings
        /// </summary>
        [HttpGet("{id}/ratings")]
        [Authorize(Policy = "CanRateStudents")] // Limit to Staff/Directors?
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetStudentRatings(int id)
        {
            var ratings = await _unitOfWork.StudentRatings.GetQueryable()
                .Include(r => r.BandStaff)
                .Where(r => r.StudentId == id)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new RatingDto
                {
                    Rating = r.Rating,
                    Comments = r.Comments
                })
                .ToListAsync();

            return Ok(ratings);
        }

        /// <summary>
        /// Matches frontend: uploadVideo(studentId, formData)
        /// Endpoint: POST api/Students/{id}/video
        /// Handles direct file upload from frontend.
        /// </summary>
        [HttpPost("{id}/video")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> UploadVideo(int id, [FromForm] IFormFile file, [FromForm] string title, [FromForm] string instrument)
        {
            // 1. Verify Ownership
            if (!await _permissionService.IsStudentOwnerAsync(id)) return Forbid();

            // 2. Validate File
            if (file == null || file.Length == 0) return BadRequest("No file uploaded");

            var validation = await _videoService.ValidateVideoUploadAsync(id, file.Length);
            if (!validation.success) return BadRequest(validation.message);

            // 3. Upload Logic
            // WARNING: IVideoStorageService currently handles Presigned URLs. 
            // You must extend it to support direct stream uploads (e.g., UploadStreamAsync).

            var storageFileName = $"{id}/{Guid.NewGuid()}_{file.FileName}";

            try
            {
                using var stream = file.OpenReadStream();
                // AWAITING IMPLEMENTATION: 
                // await _storageService.UploadStreamAsync(stream, storageFileName, file.ContentType);

                // For now, this line throws because the method doesn't exist in your interface yet:
                throw new NotImplementedException("IVideoStorageService needs an UploadStreamAsync method to support direct uploads.");
            }
            catch (NotImplementedException ex)
            {
                return StatusCode(501, ex.Message);
            }

            // 4. Create Video Entity
            var createRequest = new CreateVideoRequest
            {
                UploadId = storageFileName, // Acts as the storage key
                Title = title,
                Instrument = instrument ?? "Unknown",
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSizeBytes = file.Length,
                IsPublic = true
            };

            var video = await _videoService.CreateVideoAsync(id, createRequest);

            return CreatedAtAction("GetVideo", "Video", new { id = video.Id }, video);
        }

        /// <summary>
        /// Matches frontend: deleteVideo(studentId, videoId)
        /// Endpoint: DELETE api/Students/{id}/video/{videoId}
        /// </summary>
        [HttpDelete("{id}/video/{videoId}")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> DeleteVideo(int id, int videoId)
        {
            if (!await _permissionService.IsStudentOwnerAsync(id)) return Forbid();

            var success = await _videoService.SoftDeleteVideoAsync(videoId, id);
            if (!success) return NotFound("Video not found");

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