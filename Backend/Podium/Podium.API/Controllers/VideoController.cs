using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Video;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Interfaces;
using System.Security.Claims;

namespace Podium.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VideoController : ControllerBase
    {
        private readonly IVideoService _videoService;
        private readonly IVideoStorageService _storageService;
        private readonly IPermissionService _permissionService;
        private readonly ILogger<VideoController> _logger;

        public VideoController(
            IVideoService videoService,
            IVideoStorageService storageService,
            IPermissionService permissionService,
            ILogger<VideoController> logger)
        {
            _videoService = videoService;
            _storageService = storageService;
            _permissionService = permissionService;
            _logger = logger;
        }

        /// <summary>
        /// Step 1: Request a secure upload URL (SAS/Presigned) for the file.
        /// Student Only.
        /// </summary>
        [HttpPost("upload-url")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<GetUploadResponse>> RequestUploadUrl([FromBody] GetUploadRequest request)
        {
            var userIdStr = this.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // 1. Validate quotas/sizes
            var validation = await _videoService.ValidateVideoUploadAsync(userId, request.FileSizeBytes);
            if (!validation.success)
            {
                return BadRequest(validation.message);
            }

            // 2. Generate a unique storage path: {studentId}/{guid}_{filename}
            var storageFileName = $"{userId}/{Guid.NewGuid()}_{request.FileName}";

            // 3. Generate the Pre-signed URL / SAS Token
            var uploadUrl = await _storageService.GenerateUploadUrlAsync(storageFileName, request.ContentType);

            return Ok(new GetUploadResponse
            {
                UploadUrl = uploadUrl,
                StoragePath = storageFileName,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60)
            });
        }

        /// <summary>
        /// Step 2: Confirm video creation after successful upload.
        /// Student Only.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<VideoResponse>> CreateVideo([FromBody] CreateVideoRequest request)
        {
            var userIdStr = this.GetCurrentUserId();
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return Unauthorized();

            // The 'FileName' in request should match the 'StoragePath' returned in Step 1
            // You might want to validate this matches the expected pattern

            var video = await _videoService.CreateVideoAsync(userId, request);

            return CreatedAtAction(nameof(GetVideo), new { id = video.Id }, new VideoResponse
            {
                VideoId = video.Id,
                Title = video.Title,
                VideoUrl = await _storageService.GetVideoUrlAsync(video.Url) // Return a viewable link immediately
            });
        }

        /// <summary>
        /// Get a specific video with a secure playback link.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<VideoResponse>> GetVideo(int id)
        {
            var userIdStr = this.GetCurrentUserId();
            int.TryParse(userIdStr, out int userId);
            var userRole = this.GetCurrentUserRole() ?? string.Empty;

            try
            {
                var video = await _videoService.GetVideoDetailsAsync(id, userId == 0 ? null : userId, userRole);
                return Ok(video);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Get current student's videos.
        /// </summary>
        [HttpGet("my-videos")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<List<MyVideoListItem>>> GetMyVideos()
        {
            var userIdStr = this.GetCurrentUserId();
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var videos = await _videoService.GetMyVideosAsync(userId);
            return Ok(videos);
        }

        /// <summary>
        /// Update video metadata.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> UpdateVideo(int id, [FromBody] UpdateVideoRequest request)
        {
            var userIdStr = this.GetCurrentUserId();
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            try
            {
                await _videoService.UpdateVideoAsync(id, userId, request);
                return NoContent();
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>
        /// Soft delete a video.
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var userIdStr = this.GetCurrentUserId();
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var result = await _videoService.SoftDeleteVideoAsync(id, userId);
            if (!result) return NotFound();

            return NoContent();
        }

        /// <summary>
        /// Rate a video.
        /// Only BandStaff (Recruiters/Directors) with 'CanRateStudents' permission.
        /// </summary>
        [HttpPost("{id}/rate")]
        [Authorize(Policy = "CanRateStudents")]
        public async Task<ActionResult<VideoRatingResponse>> RateVideo(int id, [FromBody] RateVideoRequest request)
        {
            var userIdStr = this.GetCurrentUserId();
            if (!int.TryParse(userIdStr, out int recruiterId)) return Unauthorized();

            // We need to verify the recruiter belongs to the band the student is interested in?
            // Or usually, recruiters can rate any visible student in the system (Platform wide vs Band specific)
            // Assuming Platform wide visibility for simplicity, or Service handles filtering.

            try
            {
                var response = await _videoService.RateVideoAsync(id, recruiterId, request);
                return Ok(response);
            }
            catch (KeyNotFoundException) { return NotFound(); }
        }

        /// <summary>
        /// Increment view count (public endpoint if video is public, otherwise authorized).
        /// </summary>
        [HttpPost("{id}/view")]
        [AllowAnonymous]
        public async Task<IActionResult> IncrementViewCount(int id)
        {
            var success = await _videoService.IncrementViewCountAsync(id);
            if (!success) return NotFound();
            return Ok();
        }

        /// <summary>
        /// Optional: Webhook for Transcoding services (Azure Media Services / AWS MediaConvert).
        /// Secured via a secret query param or header in production.
        /// </summary>
        [HttpPost("webhook/transcoding")]
        [AllowAnonymous] // Should be protected by a specific API Key or Secret check inside
        public async Task<IActionResult> TranscodingWebhook([FromBody] TranscodingWebhookRequest request, [FromQuery] string secret)
        {
            // Simple security check example
            // if (secret != _configuration["TranscodingWebhookSecret"]) return Unauthorized();

            await _videoService.UpdateTranscodingStatusAsync(request.JobId, request);
            return Ok();
        }

        /// <summary>
        /// Get all ratings for a specific video.
        /// Only accessible by BandStaff (Recruiters/Directors).
        /// </summary>
        [HttpGet("{id}/ratings")]
        [Authorize(Policy = "CanRateStudents")] // Or "CanViewStudents" depending on your preference
        public async Task<ActionResult<List<VideoRatingResponse>>> GetVideoRatings(int id)
        {
            try
            {
                var ratings = await _videoService.GetVideoRatingsAsync(id);
                return Ok(ratings);
            }
            catch (KeyNotFoundException)
            {
                return NotFound("Video not found");
            }
        }

        /// <summary>
        /// Update an existing rating.
        /// Only the staff member who created the rating should be able to update it.
        /// </summary>
        [HttpPut("{id}/rate")]
        [Authorize(Policy = "CanRateStudents")]
        public async Task<ActionResult<VideoRatingResponse>> UpdateRating(int id, [FromBody] RateVideoRequest request)
        {
            var userIdStr = this.GetCurrentUserId();
            if (!int.TryParse(userIdStr, out int recruiterId)) return Unauthorized();

            try
            {
                var response = await _videoService.UpdateRatingAsync(id, recruiterId, request);
                return Ok(response);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                // Thrown if the recruiter tries to update someone else's rating
                return Forbid(ex.Message);
            }
        }
    }
}