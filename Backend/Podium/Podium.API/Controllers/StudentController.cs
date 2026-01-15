using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Rating;
using Podium.Application.DTOs.Student;
using Podium.Application.DTOs.Video; // Required for CreateVideoRequest
using Podium.Application.Interfaces;
using Podium.Application.Services;
using Podium.Core.Constants;
using Podium.Core.Entities;
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
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IUnitOfWork unitOfWork,
            IPermissionService permissionService,
            IStudentService studentService,
            IVideoService videoService,
            IVideoStorageService storageService,
            ILogger<StudentsController> logger)
        {
            _unitOfWork = unitOfWork;
            _permissionService = permissionService;
            _studentService = studentService;
            _videoService = videoService;
            _storageService = storageService;
            _logger = logger;
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

        [HttpGet("dashboard")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<StudentDashboardDto>> GetDashboard()
        {
            // Assuming you have a method in _studentService to get dashboard data
            // If not, you might need to build the DTO here or add the method to the service
            var result = await _studentService.GetStudentDashboardAsync(); // You need to implement this in StudentService
            return Ok(result);
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
        /// Advanced student search with comprehensive filtering
        /// </summary>
        [HttpGet("search")]
        [ProducesResponseType(typeof(StudentSearchResponseDto), 200)]
        public async Task<ActionResult<StudentSearchResponseDto>> SearchStudents(
            [FromQuery] string? searchTerm,
            [FromQuery] List<string>? instruments,
            [FromQuery] List<string>? states,
            [FromQuery] bool? isHBCU,
            [FromQuery] int? distance,
            [FromQuery] string? zipCode,
            [FromQuery] double? minGPA,
            [FromQuery] double? maxGPA,
            [FromQuery] List<int>? graduationYears,
            [FromQuery] List<string>? majors,
            [FromQuery] List<string>? skillLevels,
            [FromQuery] int? minYearsExperience,
            [FromQuery] int? maxYearsExperience,
            [FromQuery] bool? hasVideo,
            [FromQuery] bool? hasAuditionVideo,
            [FromQuery] bool? isAvailable,
            [FromQuery] bool? isActivelyRecruiting,
            [FromQuery] bool? hasScholarshipOffers,
            [FromQuery] int? lastActivityDays,
            [FromQuery] string sortBy = "relevance",
            [FromQuery] string sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _unitOfWork.Students.GetQueryable()
                    .Include(s => s.Videos)
                    .Include(s => s.StudentRatings)
                    .Include(s => s.ScholarshipOffers)
                    .Where(s => !s.IsDeleted);

                // Text search
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var term = searchTerm.ToLower();
                    query = query.Where(s =>
                        s.FirstName.ToLower().Contains(term) ||
                        s.LastName.ToLower().Contains(term) ||
                        s.PrimaryInstrument.ToLower().Contains(term) ||
                        s.City.ToLower().Contains(term) ||
                        s.State.ToLower().Contains(term) ||
                        s.HighSchool.ToLower().Contains(term)
                    );
                }

                // Instrument filter
                if (instruments != null && instruments.Any())
                {
                    query = query.Where(s =>
                        instruments.Contains(s.PrimaryInstrument) ||
                        (s.SecondaryInstruments != null && instruments.Any(i => s.SecondaryInstruments.Contains(i)))
                    );
                }

                // Location filters
                if (states != null && states.Any())
                {
                    query = query.Where(s => states.Contains(s.State));
                }

                // HBCU filter - would need to check student's interested bands
                if (isHBCU == true)
                {
                    // This would require joining with Bands and checking IsHBCU flag
                    // For now, placeholder
                }

                // GPA filters
                if (minGPA.HasValue)
                {
                    query = query.Where(s => s.GPA >= (decimal)minGPA.Value);
                }

                if (maxGPA.HasValue)
                {
                    query = query.Where(s => s.GPA <= (decimal)maxGPA.Value);
                }

                // Graduation year
                if (graduationYears != null && graduationYears.Any())
                {
                    query = query.Where(s => graduationYears.Contains(s.GraduationYear));
                }

                // Majors
                if (majors != null && majors.Any())
                {
                    query = query.Where(s => majors.Contains(s.IntendedMajor));
                }

                // Skill levels
                if (skillLevels != null && skillLevels.Any())
                {
                    query = query.Where(s => skillLevels.Contains(s.SkillLevel));
                }

                // Years of experience
                if (minYearsExperience.HasValue)
                {
                    query = query.Where(s => s.YearsExperience >= minYearsExperience.Value);
                }

                if (maxYearsExperience.HasValue)
                {
                    query = query.Where(s => s.YearsExperience <= maxYearsExperience.Value);
                }

                // Video filters
                if (hasVideo == true)
                {
                    query = query.Where(s => s.Videos.Any());
                }

                if (hasAuditionVideo == true)
                {
                    query = query.Where(s => s.Videos.Any(v => v.IsAuditionVideo));
                }

                // Availability
                if (isAvailable == true)
                {
                    query = query.Where(s => s.IsAvailableForRecruiting);
                }

                // Recent activity
                if (lastActivityDays.HasValue)
                {
                    var cutoffDate = DateTime.UtcNow.AddDays(-lastActivityDays.Value);
                    query = query.Where(s => s.LastActivityDate >= cutoffDate);
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Sorting
                query = sortBy.ToLower() switch
                {
                    "gpa" => sortDirection == "asc"
                        ? query.OrderBy(s => s.GPA)
                        : query.OrderByDescending(s => s.GPA),
                    "experience" => sortDirection == "asc"
                        ? query.OrderBy(s => s.YearsExperience)
                        : query.OrderByDescending(s => s.YearsExperience),
                    "recent" => query.OrderByDescending(s => s.LastActivityDate),
                    "rating" => query.OrderByDescending(s => s.StudentRatings.Any() ? s.StudentRatings.Average(r => r.Rating) : 0),
                    "name" => sortDirection == "asc"
                        ? query.OrderBy(s => s.FirstName).ThenBy(s => s.LastName)
                        : query.OrderByDescending(s => s.FirstName).ThenByDescending(s => s.LastName),
                    _ => query.OrderByDescending(s => s.Id) // Default relevance
                };

                // Pagination
                var students = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new StudentSearchResultDto
                {
                    StudentId = s.Id,
                    FirstName = s.FirstName,
                    LastName = s.LastName,
                    FullName = s.FirstName + " " + s.LastName,
                    ProfilePhotoUrl = "", // Add this property to Student if needed
                    PrimaryInstrument = s.PrimaryInstrument ?? "",
                    SecondaryInstruments = s.SecondaryInstruments != null
                                            ? s.SecondaryInstruments.ToArray()
                                            : new string[0],
                    SkillLevel = s.SkillLevel ?? "",
                    YearsOfExperience = s.YearsExperience ?? 0,
                    City = "", // Add this property to Student if needed
                    State = s.State ?? "",
                    ZipCode = "", // Add this property to Student if needed
                    GPA = (double?)s.GPA ?? 0,
                    GraduationYear = s.GraduationYear,
                    IntendedMajor = s.IntendedMajor ?? "",
                    HighSchool = s.HighSchool ?? "",
                    VideoCount = s.Videos.Count,
                    HasAuditionVideo = s.Videos.Any(v => v.IsAuditionVideo),
                    ProfileViews = 0, // Add this property to Student if needed
                    AverageRating = s.StudentRatings.Any() ? s.StudentRatings.Average(r => r.Rating) : null,
                    RatingCount = s.StudentRatings.Count,
                    AccountStatus = "Active", // Hardcoded since property doesn't exist
                    IsAvailableForRecruiting = true, // Add this property to Student if needed
                    LastActivityDate = s.LastActivityDate,
                    IsWatchlisted = false, // Would check against recruiter's watchlist
                    HasSentOffer = false, // Would check if current recruiter has sent offer
                    HasContactRequest = false // Would check if current recruiter has pending request
                })
                .ToListAsync();

                var response = new StudentSearchResponseDto
                {
                    Results = students,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    AppliedFiltersCount = CountAppliedFilters(
                        searchTerm, instruments, states, isHBCU, minGPA, maxGPA,
                        graduationYears, skillLevels, hasVideo, isAvailable)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching students");
                return StatusCode(500, "An error occurred while searching students");
            }
        }


        /// <summary>
        /// Optimized endpoint for fetching student cards using database projection
        /// </summary>
        [HttpGet("cards")]
        [Authorize(Policy = "BandStaffOnly")] // Or generic view policy
        public async Task<ActionResult<PagedResult<StudentCardDto>>> GetStudentCards(
            [FromQuery] string? instrument,
            [FromQuery] double? minGpa,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _studentService.GetStudentCardsAsync(instrument, minGpa, page, pageSize);

            if (result.IsSuccess) return Ok(result.Data);

            return result.ResultType switch
            {
                ServiceResultType.Forbidden => Forbid(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };
        }

        /// <summary>
        /// Count how many filters are applied
        /// </summary>
        private int CountAppliedFilters(
            string? searchTerm,
            List<string>? instruments,
            List<string>? states,
            bool? isHBCU,
            double? minGPA,
            double? maxGPA,
            List<int>? graduationYears,
            List<string>? skillLevels,
            bool? hasVideo,
            bool? isAvailable)
        {
            int count = 0;

            if (!string.IsNullOrEmpty(searchTerm)) count++;
            if (instruments != null && instruments.Any()) count++;
            if (states != null && states.Any()) count++;
            if (isHBCU == true) count++;
            if (minGPA.HasValue || maxGPA.HasValue) count++;
            if (graduationYears != null && graduationYears.Any()) count++;
            if (skillLevels != null && skillLevels.Any()) count++;
            if (hasVideo == true) count++;
            if (isAvailable == true) count++;

            return count;
        }


        /// <summary>
        /// Get search suggestions for autocomplete
        /// </summary>
        [HttpGet("search/suggestions")]
        [ProducesResponseType(typeof(List<SearchSuggestionDto>), 200)]
        public async Task<ActionResult<List<SearchSuggestionDto>>> GetSearchSuggestions(
            [FromQuery] string term,
            [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
            {
                return Ok(new List<SearchSuggestionDto>());
            }

            var suggestions = new List<SearchSuggestionDto>();
            var lowerTerm = term.ToLower();

            // Student names
            var studentNames = await _unitOfWork.Students.GetQueryable()
                .Where(s => s.FirstName.ToLower().Contains(lowerTerm) ||
                           s.LastName.ToLower().Contains(lowerTerm))
                .Take(5)
                .Select(s => new SearchSuggestionDto
                {
                    Text = s.FirstName + " " + s.LastName,
                    Type = "student"
                })
                .ToListAsync();

            suggestions.AddRange(studentNames);

            // Instruments (from predefined list)
            var instruments = new[] { "Trumpet", "Saxophone", "Trombone", "Flute", "Clarinet", "Drum" }
                .Where(i => i.ToLower().Contains(lowerTerm))
                .Select(i => new SearchSuggestionDto { Text = i, Type = "instrument" })
                .Take(3);

            suggestions.AddRange(instruments);

            return Ok(suggestions.Take(limit).ToList());
        }

        private int CountAppliedFilters(params object?[] filters)
        {
            return filters.Count(f => f != null && (
                (f is string s && !string.IsNullOrEmpty(s)) ||
                (f is IEnumerable<object> e && e.Any()) ||
                (f is bool) ||
                (f is int) ||
                (f is double)
            ));
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