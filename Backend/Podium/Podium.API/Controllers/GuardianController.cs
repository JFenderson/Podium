using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Application.DTOs.Guardian;
using Podium.Application.Interfaces;
using Podium.Application.Services;
using Podium.Core.Constants;
using Podium.Core.Entities;
using System.Security.Claims;

namespace BandRecruitment.Controllers
{
    /// <summary>
    /// Controller for parent/guardian oversight of linked students.
    /// Provides monitoring, approval workflows, and notification management.
    /// </summary>
    [Authorize(Roles = Roles.Guardian)]
    [ApiController]
    [Route("api/[controller]")]
    public class GuardianController : ControllerBase
    {
        private readonly IGuardianService _guardianService;
        private readonly IAuditService _auditService;
        private readonly ILogger<GuardianController> _logger;

        public GuardianController(
            IGuardianService guardianService,
            IAuditService auditService,
            ILogger<GuardianController> logger)
        {
            _guardianService = guardianService;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Get all students linked to this guardian.
        /// Returns basic profile information and link status.
        /// Authorization: Only returns students explicitly linked to this guardian.
        /// </summary>
        [HttpGet("students")]
        [ProducesResponseType(typeof(List<LinkedStudentDto>), 200)]
        public async Task<ActionResult<List<LinkedStudentDto>>> GetLinkedStudents()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var students = await _guardianService.GetLinkedStudentsAsync(userId);

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving linked students for guardian {UserId}", User.Identity?.Name);
                return StatusCode(500, "An error occurred while retrieving linked students");
            }
        }

        /// <summary>
        /// Get recent activity for a specific student.
        /// Includes: videos uploaded, interest shown in bands, scholarship offers received.
        /// Authorization: Guardian must be linked to the student with monitoring permissions.
        /// </summary>
        [HttpGet("student/{studentId}/activity")]
        [ProducesResponseType(typeof(StudentActivityDto), 200)]
        public async Task<ActionResult<StudentActivityDto>> GetStudentActivity(
            int studentId,
            [FromQuery] int daysBack = 30)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization: Verify guardian has access to this student
                if (!await _guardianService.CanAccessStudentAsync(userId, studentId))
                {
                    await _auditService.LogUnauthorizedAccessAsync(userId, "StudentActivity", studentId);
                    return Forbid("You do not have access to this student's information");
                }

                var activity = await _guardianService.GetStudentActivityAsync(studentId, daysBack);

                return Ok(activity);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving activity for student {StudentId}", studentId);
                return StatusCode(500, "An error occurred while retrieving student activity");
            }
        }

        /// <summary>
        /// Get detailed profile information for a linked student.
        /// Authorization: Guardian must be linked to the student.
        /// Privacy: Only returns information the student has permitted guardians to view.
        /// </summary>
        [HttpGet("student/{studentId}/profile")]
        [ProducesResponseType(typeof(StudentProfileDto), 200)]
        public async Task<ActionResult<StudentProfileDto>> GetStudentProfile(int studentId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization check
                if (!await _guardianService.CanAccessStudentAsync(userId, studentId))
                {
                    return Forbid("You do not have access to this student's profile");
                }

                var profile = await _guardianService.GetStudentProfileAsync(studentId);

                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for student {StudentId}", studentId);
                return StatusCode(500, "An error occurred while retrieving student profile");
            }
        }

        /// <summary>
        /// Get all pending contact requests for linked students.
        /// Returns requests that require guardian approval before recruiters can contact students.
        /// Authorization: Only shows requests for students linked to this guardian.
        /// Performance: Optimized query with includes for band and recruiter details.
        /// </summary>
        [HttpGet("contact-requests")]
        [ProducesResponseType(typeof(List<ContactRequestDto>), 200)]
        public async Task<ActionResult<List<ContactRequestDto>>> GetContactRequests(
            [FromQuery] int? studentId,
            [FromQuery] string? status)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // If studentId is provided, verify guardian has access
                if (studentId.HasValue && !await _guardianService.CanAccessStudentAsync(userId, studentId.Value))
                {
                    return Forbid("You do not have access to contact requests for this student");
                }

                var requests = await _guardianService.GetContactRequestsAsync(userId, studentId, status);

                return Ok(requests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving contact requests");
                return StatusCode(500, "An error occurred while retrieving contact requests");
            }
        }

        /// <summary>
        /// Approve a contact request, allowing recruiter to contact the student.
        /// Authorization: Guardian must be linked to the student in the request.
        /// Business Logic: Updates request status, triggers notification to recruiter and student.
        /// Audit: Logs approval for compliance tracking.
        /// </summary>
        [HttpPut("contact-requests/{id}/approve")]
        [ProducesResponseType(typeof(ContactRequestDto), 200)]
        public async Task<ActionResult<ContactRequestDto>> ApproveContactRequest(
            int id,
            [FromBody] ContactRequestResponseDto? response)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization: Verify guardian can approve this request
                if (!await _guardianService.CanManageContactRequestAsync(userId, id))
                {
                    return Forbid("You do not have permission to approve this contact request");
                }

                var approvedRequest = await _guardianService.ApproveContactRequestAsync(id, userId, response?.Notes);

                // Audit logging
                await _auditService.LogActionAsync(
                    userId,
                    "ContactRequestApproved",
                    $"Approved contact request {id}",
                    new { RequestId = id, StudentId = approvedRequest.StudentId, BandId = approvedRequest.BandId });

                return Ok(approvedRequest);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving contact request {RequestId}", id);
                return StatusCode(500, "An error occurred while approving contact request");
            }
        }

        /// <summary>
        /// Decline a contact request with optional reason.
        /// Authorization: Guardian must be linked to the student.
        /// Business Logic: Updates status, notifies recruiter of decline (without exposing guardian identity if privacy enabled).
        /// </summary>
        [HttpPut("contact-requests/{id}/decline")]
        [ProducesResponseType(typeof(ContactRequestDto), 200)]
        public async Task<ActionResult<ContactRequestDto>> DeclineContactRequest(
            int id,
            [FromBody] ContactRequestResponseDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization check
                if (!await _guardianService.CanManageContactRequestAsync(userId, id))
                {
                    return Forbid("You do not have permission to decline this contact request");
                }

                var declinedRequest = await _guardianService.DeclineContactRequestAsync(id, userId, request.Reason);

                // Audit logging
                await _auditService.LogActionAsync(
                    userId,
                    "ContactRequestDeclined",
                    $"Declined contact request {id}",
                    new { RequestId = id, Reason = request.Reason });

                return Ok(declinedRequest);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining contact request {RequestId}", id);
                return StatusCode(500, "An error occurred while declining contact request");
            }
        }

        /// <summary>
        /// Get all scholarship offers for linked students.
        /// Includes offer details, amounts, deadlines, and response status.
        /// Authorization: Only returns offers for students linked to this guardian.
        /// </summary>
        [HttpGet("scholarships")]
        [ProducesResponseType(typeof(List<GuardianScholarshipDto>), 200)]
        public async Task<ActionResult<List<GuardianScholarshipDto>>> GetScholarships(
            [FromQuery] int? studentId,
            [FromQuery] string? status)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // If filtering by specific student, verify access
                if (studentId.HasValue && !await _guardianService.CanAccessStudentAsync(userId, studentId.Value))
                {
                    return Forbid("You do not have access to scholarships for this student");
                }

                var scholarships = await _guardianService.GetScholarshipsAsync(userId, studentId, status);

                return Ok(scholarships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scholarships");
                return StatusCode(500, "An error occurred while retrieving scholarships");
            }
        }

        /// <summary>
        /// Respond to a scholarship offer (accept or decline).
        /// Authorization: Guardian must have scholarship decision permissions for the student.
        /// Business Logic: 
        /// - Validates response deadline hasn't passed
        /// - Updates offer status
        /// - Triggers notifications to band and student
        /// - For acceptance, may initiate enrollment workflow
        /// Audit: Logs all scholarship decisions for financial tracking.
        /// </summary>
        [HttpPut("scholarships/{id}/respond")]
        [ProducesResponseType(typeof(GuardianScholarshipDto), 200)]
        public async Task<ActionResult<GuardianScholarshipDto>> RespondToScholarship(
            int id,
            [FromBody] ScholarshipResponseRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Authorization: Verify guardian has permission to respond to this offer
                if (!await _guardianService.CanRespondToScholarshipAsync(userId, id))
                {
                    return Forbid("You do not have permission to respond to this scholarship offer");
                }

                var updatedOffer = await _guardianService.RespondToScholarshipAsync(
                    id,
                    userId,
                    request.Response,
                    request.Notes);

                // Audit logging for scholarship decision
                await _auditService.LogActionAsync(
                    userId,
                    $"ScholarshipOffer{request.Response}",
                    $"{request.Response} scholarship offer {id}",
                    new { OfferId = id, Response = request.Response, StudentId = updatedOffer.StudentId });

                return Ok(updatedOffer);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error responding to scholarship {OfferId}", id);
                return StatusCode(500, "An error occurred while responding to scholarship");
            }
        }

        /// <summary>
        /// Get notifications for the guardian.
        /// Includes: new offers, contact requests, student activity alerts.
        /// Supports filtering by type, read status, and date range.
        /// Performance: Paginated to handle large notification volumes.
        /// </summary>
        [HttpGet("notifications")]
        [ProducesResponseType(typeof(NotificationListDto), 200)]
        public async Task<ActionResult<NotificationListDto>> GetNotifications(
             [FromQuery] string? type,
             [FromQuery] bool? isRead,
             [FromQuery] DateTime? since,
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User ID not found in token");

            var filters = new NotificationFilterDto
            {
                Type = type,
                IsRead = isRead,
                Since = since,
                Page = page,
                PageSize = pageSize
            };

            var result = await _guardianService.GetNotificationsAsync(userId, filters);
            // FIX: Explicitly specify <NotificationListDto>
            return HandleResult<NotificationListDto>(result);
        }

        /// <summary>
        /// Update guardian notification preferences.
        /// Allows customization of which events trigger notifications and delivery methods (email, SMS, in-app).
        /// Preferences are stored per student link to allow granular control.
        /// </summary>
        [HttpPut("preferences")]
        [ProducesResponseType(typeof(GuardianNotificationPreferencesDto), 200)]
        public async Task<ActionResult<GuardianNotificationPreferencesDto>> UpdatePreferences(
            [FromBody] UpdatePreferencesRequest request)
        {
            try
            {
                if (!ModelState.IsValid) return BadRequest(ModelState);

                var result = await _guardianService.UpdateNotificationPreferencesAsync(request);
                // FIX: Explicitly specify <GuardianNotificationPreferencesDto>
                return HandleResult<GuardianNotificationPreferencesDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification preferences");
                return StatusCode(500, "An error occurred while updating preferences");
            }
        }

        /// <summary>
        /// Get comprehensive dashboard with overview of all linked students.
        /// Includes:
        /// - Summary statistics (active offers, pending approvals, recent activity)
        /// - Per-student quick view
        /// - Priority alerts (expiring offers, urgent contact requests)
        /// Performance: Single optimized query with strategic includes and projections.
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(GuardianDashboardDto), 200)]
        public async Task<ActionResult<GuardianDashboardDto>> GetDashboard()
        {
            try
            {
                var result = await _guardianService.GetDashboardAsync();
                // FIX: Explicitly specify <GuardianDashboardDto>
                return HandleResult<GuardianDashboardDto>(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving guardian dashboard");
                return StatusCode(500, "An error occurred while retrieving dashboard");
            }
        }

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