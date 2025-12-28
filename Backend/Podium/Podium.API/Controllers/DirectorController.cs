using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.Authorization;
using Podium.Application.DTOs.Band;
using Podium.Application.DTOs.BandEvent;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Director;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.Student;
using Podium.Application.Interfaces;
using Podium.Core.Constants;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;
using System.Security.Claims;

namespace BandRecruitment.Controllers
{
    /// <summary>
    /// Controller for band director administrative functions.
    /// Provides comprehensive band management, staff oversight, analytics, and scholarship administration.
    /// </summary>
    [Authorize(Roles = Roles.Director)]
    [ApiController]
    [Route("api/[controller]")]
    public class DirectorController : ControllerBase
    {
        private readonly IDirectorService _directorService;
        private readonly IAuditService _auditService;
        private readonly ILogger<DirectorController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPermissionService _permissionService;

        public DirectorController(
            IDirectorService directorService,
            IAuditService auditService,
            ILogger<DirectorController> logger,
            IPermissionService permissionService,
            IUnitOfWork unitOfWork)
        {
            _directorService = directorService;
            _auditService = auditService;
            _logger = logger;

            _permissionService = permissionService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Get current Director's information
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<BandStaffDto>> GetMyInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var staffList = await _directorService.GetStaffMembersAsync(userId, null, null);
            var me = staffList.FirstOrDefault(s => s.ApplicationUserId == userId);

            if (me == null) return NotFound("Staff profile not found");

            return Ok(me);
        }


        /// <summary>
        /// Get complete director dashboard
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(DirectorDashboardDto), 200)]
        public async Task<ActionResult<DirectorDashboardDto>> GetDashboard(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? recruiterId,
            [FromQuery] string? instrument,
            [FromQuery] string? offerStatus)
        {
            try
            {
                // 1. Get userId
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                // 2. Get bandId
                var bandId = await GetDirectorBandIdAsync(userId);
                if (bandId == null)
                    return NotFound("Director band not found");

                var start = startDate ?? DateTime.UtcNow.AddDays(-30);
                var end = endDate ?? DateTime.UtcNow;

                // Get all data in parallel
                var metricsTask = await _directorService.GetKeyMetricsAsync(userId, start, end);
                var funnelTask = await _directorService.GetRecruitmentFunnelAsync(bandId.Value, start, end);
                var offersTask = await _directorService.GetOffersOverviewAsync(bandId.Value, start, end);
                var staffTask = await _directorService.GetStaffPerformanceAsync(bandId.Value, start, end);
                var approvalsTask = await _directorService.GetPendingApprovalsAsync(bandId.Value);
                var activityTask = await _directorService.GetRecentActivityAsync(bandId.Value, 20);


                var dashboard = new DirectorDashboardDto
                {
                    KeyMetrics = metricsTask,
                    RecruitmentFunnel = funnelTask,
                    OffersOverview = offersTask,
                    StaffPerformance = staffTask,
                    PendingApprovals = approvalsTask,
                    RecentActivity = activityTask,
                    DateRangeStart = start,
                    DateRangeEnd = end
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading director dashboard");
                return StatusCode(500, "An error occurred while loading the dashboard");
            }
        }

        /// <summary>
        /// Get key metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult<DirectorKeyMetricsDto>> GetMetrics(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var metrics = await _directorService.GetKeyMetricsAsync(userId, start, end);
            return Ok(metrics);
        }

        /// <summary>
        /// Get recruitment funnel
        /// </summary>
        [HttpGet("funnel")]
        public async Task<ActionResult<List<FunnelStageDto>>> GetFunnel(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            // 1. Get userId
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. Get bandId
            var bandId = await GetDirectorBandIdAsync(userId);
            if (bandId == null)
                return NotFound("Director band not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var funnel = await _directorService.GetRecruitmentFunnelAsync(bandId.Value, start, end);
            return Ok(funnel);
        }

        /// <summary>
        /// Get students in a specific funnel stage
        /// </summary>
        [HttpGet("funnel/{stage}/students")]
        public async Task<ActionResult<List<FunnelStudentDto>>> GetFunnelStageStudents(
            string stage,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            // Implementation would query students based on stage
            // For now, returning placeholder
            return Ok(new List<FunnelStudentDto>());
        }


        /// <summary>
        /// Get detailed analytics for a specific band.
        /// Includes student interest trends, scholarship budget utilization, conversion rates, and demographics.
        /// Authorization: Director must be associated with the requested band.
        /// </summary>
        [HttpGet("band/{bandId}/analytics")]
        [ProducesResponseType(typeof(BandAnalyticsDto), 200)]
        public async Task<ActionResult<BandAnalyticsDto>> GetBandAnalytics(
            int bandId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization check: Verify director manages this band
                if (!await _directorService.CanAccessBandAsync(userId, bandId))
                {
                    await _auditService.LogUnauthorizedAccessAsync(userId, "BandAnalytics", bandId);
                    return Forbid("You do not have access to this band's analytics");
                }

                var analytics = await _directorService.GetBandAnalyticsAsync(
                    bandId,
                    startDate ?? DateTime.UtcNow.AddMonths(-6),
                    endDate ?? DateTime.UtcNow);

                return Ok(analytics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving analytics for band {BandId}", bandId);
                return StatusCode(500, "An error occurred while retrieving analytics");
            }
        }

        /// <summary>
        /// Add a new staff member (recruiter) to the band.
        /// Creates BandStaff relationship with specified permissions.
        /// Audit: Logs all staff additions for compliance.
        /// </summary>
        [HttpPost("staff")]
        [ProducesResponseType(typeof(BandStaffDto), 201)]
        public async Task<ActionResult<BandStaffDto>> AddStaff([FromBody] CreateBandStaffDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Validate request
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Authorization: Verify director manages the band
                if (!await _directorService.CanAccessBandAsync(userId, request.BandId))
                {
                    return Forbid("You do not have permission to add staff to this band");
                }

                var staffMember = await _directorService.AddStaffMemberAsync(userId, request);

                // Audit logging for staff addition
                await _auditService.LogActionAsync(
                    userId,
                    "StaffAdded",
                    $"Added staff member {request.ApplicationUserId} to band {request.BandId}",
                    new { request.BandId, request.ApplicationUserId, request.Permissions });

                return CreatedAtAction(
                    nameof(GetStaff),
                    new { staffId = staffMember.BandStaffId },
                    staffMember);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid staff addition attempt");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding staff member");
                return StatusCode(500, "An error occurred while adding staff member");
            }
        }

        /// <summary>
        /// Update staff member permissions and role.
        /// Authorization: Director must manage the band this staff member belongs to.
        /// Audit: Logs permission changes for security tracking.
        /// </summary>
        [HttpPut("staff/{staffId}")]
        [ProducesResponseType(typeof(BandStaffDto), 200)]
        public async Task<ActionResult<BandStaffDto>> UpdateStaff(
            int staffId,
            [FromBody] UpdateBandStaffDto request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                // Authorization: Verify director manages band that staff belongs to
                if (!await _directorService.CanManageStaffAsync(userId, staffId))
                {
                    return Forbid("You do not have permission to update this staff member");
                }

                var updatedStaff = await _directorService.UpdateStaffMemberAsync(staffId, request);

                // Audit logging for permission changes
                await _auditService.LogActionAsync(
                    userId,
                    "StaffUpdated",
                    $"Updated staff member {staffId}",
                    new { staffId, request.Permissions, request.IsActive });

                return Ok(updatedStaff);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating staff member {StaffId}", staffId);
                return StatusCode(500, "An error occurred while updating staff member");
            }
        }

        /// <summary>
        /// Remove a staff member from the band.
        /// Soft delete: Sets IsActive = false to preserve audit trail.
        /// Authorization: Director must manage the band.
        /// </summary>
        [HttpDelete("staff/{staffId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RemoveStaff(int staffId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization check
                if (!await _directorService.CanManageStaffAsync(userId, staffId))
                {
                    return Forbid("You do not have permission to remove this staff member");
                }

                await _directorService.RemoveStaffMemberAsync(staffId);

                // Audit logging
                await _auditService.LogActionAsync(
                    userId,
                    "StaffRemoved",
                    $"Removed staff member {staffId}",
                    new { staffId });

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing staff member {StaffId}", staffId);
                return StatusCode(500, "An error occurred while removing staff member");
            }
        }

        /// <summary>
        /// Get all staff members for the director's band with activity metrics.
        /// Includes: contact attempts, offers made, last activity timestamp.
        /// Performance: Uses efficient projections to avoid loading full entities.
        /// </summary>
        [HttpGet("staff")]
        [ProducesResponseType(typeof(List<BandStaffDto>), 200)]
        public async Task<ActionResult<List<BandStaffDto>>> GetStaff(
            [FromQuery] bool? isActive,
            [FromQuery] string? sortBy)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var staff = await _directorService.GetStaffMembersAsync(userId, isActive, sortBy);

                return Ok(staff);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving staff members");
                return StatusCode(500, "An error occurred while retrieving staff members");
            }
        }

        /// <summary>
        /// Get all scholarship offers with filtering and budget tracking.
        /// Supports filtering by status, student, date range, and amount.
        /// Includes budget utilization calculations.
        /// </summary>
        [HttpGet("scholarships")]
        [ProducesResponseType(typeof(ScholarshipOverviewDto), 200)]
        public async Task<ActionResult<ScholarshipOverviewDto>> GetScholarships(
            [FromQuery] string? status,
            [FromQuery] int? studentId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var filters = new ScholarshipFilterDto
                {
                    Status = status,
                    StudentId = studentId,
                    StartDate = startDate,
                    EndDate = endDate,
                    MinAmount = minAmount,
                    MaxAmount = maxAmount
                };

                var scholarships = await _directorService.GetScholarshipsAsync(userId, filters);

                return Ok(scholarships);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scholarships");
                return StatusCode(500, "An error occurred while retrieving scholarships");
            }
        }

        /// <summary>
        /// Approve a pending scholarship offer.
        /// Authorization: Director must manage the band that created the offer.
        /// Business Logic: Updates status, sets approval date, triggers notifications.
        /// Audit: Logs all scholarship approvals for financial tracking.
        /// </summary>
        [HttpPut("scholarships/{id}/approve")]
        [ProducesResponseType(typeof(ScholarshipOfferDto), 200)]
        public async Task<ActionResult<ScholarshipOfferDto>> ApproveScholarship(
            int id,
            [FromBody] ApproveScholarshipRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                // Authorization: Verify director can approve this offer
                if (!await _directorService.CanManageScholarshipAsync(userId, id))
                {
                    return Forbid("You do not have permission to approve this scholarship");
                }

                var approvedOffer = await _directorService.ApproveScholarshipAsync(id, userId, request.Notes);

                // Audit logging for financial approval
                await _auditService.LogActionAsync(
                    userId,
                    "ScholarshipApproved",
                    $"Approved scholarship offer {id}",
                    new { OfferId = id, Amount = approvedOffer.Amount, StudentId = approvedOffer.StudentId });

                return Ok(approvedOffer);
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
                _logger.LogError(ex, "Error approving scholarship {OfferId}", id);
                return StatusCode(500, "An error occurred while approving scholarship");
            }
        }

        /// <summary>
        /// Rescind a scholarship offer with reason.
        /// Can rescind offers in Pending or Accepted status.
        /// Authorization: Director must manage the band.
        /// Audit: Logs rescission with reason for compliance.
        /// </summary>
        [HttpPut("scholarships/{id}/rescind")]
        [ProducesResponseType(typeof(ScholarshipOfferDto), 200)]
        public async Task<ActionResult<ScholarshipOfferDto>> RescindScholarship(
            int id,
            [FromBody] RescindScholarshipRequest request)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                if (string.IsNullOrWhiteSpace(request.Reason))
                    return BadRequest("Reason is required for rescinding a scholarship");

                // Authorization check
                if (!await _directorService.CanManageScholarshipAsync(userId, id))
                {
                    return Forbid("You do not have permission to rescind this scholarship");
                }

                var rescindedOffer = await _directorService.RescindScholarshipAsync(id, userId, request.Reason);

                // Audit logging for rescission
                await _auditService.LogActionAsync(
                    userId,
                    "ScholarshipRescinded",
                    $"Rescinded scholarship offer {id}: {request.Reason}",
                    new { OfferId = id, Reason = request.Reason, StudentId = rescindedOffer.StudentId });

                return Ok(rescindedOffer);
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
                _logger.LogError(ex, "Error rescinding scholarship {OfferId}", id);
                return StatusCode(500, "An error occurred while rescinding scholarship");
            }
        }

        /// <summary>
        /// Get students who have shown interest in the band.
        /// Includes filtering by instrument, skill level, graduation year, and interest date.
        /// Returns student profiles with interest details and engagement metrics.
        /// </summary>
        [HttpGet("students/interested")]
        [ProducesResponseType(typeof(List<InterestedStudentDto>), 200)]
        public async Task<ActionResult<List<InterestedStudentDto>>> GetInterestedStudents(
            [FromQuery] string? instrument,
            [FromQuery] string? skillLevel,
            [FromQuery] int? graduationYear,
            [FromQuery] DateTime? interestedAfter,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var filters = new InterestedStudentFilterDto
                {
                    Instrument = instrument,
                    SkillLevel = skillLevel,
                    GraduationYear = graduationYear,
                    InterestedAfter = interestedAfter,
                    Page = page,
                    PageSize = pageSize
                };

                var students = await _directorService.GetInterestedStudentsAsync(userId, filters);

                return Ok(students);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving interested students");
                return StatusCode(500, "An error occurred while retrieving interested students");
            }
        }

        /// <summary>
        /// Get all events for the director's band.
        /// Includes event details, attendance counts, and registration status.
        /// Supports filtering by date range and event type.
        /// </summary>
        [HttpGet("events")]
        [ProducesResponseType(typeof(List<BandEventDto>), 200)]
        public async Task<ActionResult<List<BandEventDto>>> GetEvents(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? eventType,
            [FromQuery] bool includeArchived = false)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("User ID not found in token");

                var filters = new EventFilterDto
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    EventType = eventType,
                    IncludeArchived = includeArchived
                };

                var events = await _directorService.GetEventsAsync(userId, filters);

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, "An error occurred while retrieving events");
            }
        }

        // ==========================================
        // SPECIFIC ANALYTICS ENDPOINTS
        // ==========================================

        [HttpGet("band/{bandId}/offer-stats")]
        public async Task<ActionResult<OfferStatsDto>> GetOfferStats(int bandId)
        {
            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId)
                .Select(o => o.Status)
                .ToListAsync();

            var stats = new OfferStatsDto
            {
                TotalOffers = offers.Count,
                Pending = offers.Count(s => s == ScholarshipStatus.Sent || s == ScholarshipStatus.PendingApproval || s == ScholarshipStatus.PendingGuardianSignature),
                Accepted = offers.Count(s => s == ScholarshipStatus.Accepted),
                Declined = offers.Count(s => s == ScholarshipStatus.Declined),
                Expired = offers.Count(s => s == ScholarshipStatus.Expired)
            };

            var responses = stats.Accepted + stats.Declined;
            stats.AcceptanceRate = stats.TotalOffers > 0 ? Math.Round((decimal)stats.Accepted / stats.TotalOffers * 100, 1) : 0;
            stats.ResponseRate = stats.TotalOffers > 0 ? Math.Round((decimal)responses / stats.TotalOffers * 100, 1) : 0;

            return Ok(stats);
        }

        /// <summary>
        /// Get offers overview
        /// </summary>
        [HttpGet("offers-overview")]
        public async Task<ActionResult<OffersOverviewDto>> GetOffersOverview(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {

            // 1. Get userId
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. Get bandId
            var bandId = await GetDirectorBandIdAsync(userId);
            if (bandId == null)
                return NotFound("Director band not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var overview = await _directorService.GetOffersOverviewAsync(bandId.Value, start, end);
            return Ok(overview);
        }

        [HttpGet("band/{bandId}/engagement")]
        public async Task<ActionResult<DirectorEngagementMetricsDto>> GetEngagementMetrics(int bandId)
        {
            // 1. Get totals
            var totalInterests = await _unitOfWork.StudentInterests.GetQueryable().CountAsync(si => si.BandId == bandId);

            // Assuming you have an AuditLog for views. If not, return 0 or mock data.
            var totalViews = await _unitOfWork.AuditLogs.GetQueryable()
                .CountAsync(a => a.ActionType == "ViewBand" && a.MetadataJson.Contains(bandId.ToString()));

            // 2. Get Daily Activity (Last 7 days)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);

            // Grouping interests by date
            var dailyInterests = await _unitOfWork.StudentInterests.GetQueryable()
                .Where(si => si.BandId == bandId && si.InterestedDate >= sevenDaysAgo)
                .GroupBy(si => si.InterestedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            // Merge into a complete list
            var dailyActivity = new List<DailyEngagementDto>();
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.UtcNow.AddDays(-i).Date;
                var interestCount = dailyInterests.FirstOrDefault(d => d.Date == date)?.Count ?? 0;

                dailyActivity.Add(new DailyEngagementDto
                {
                    Date = date,
                    Interests = interestCount,
                    Views = 0 // Populate if you have daily view logs
                });
            }

            return Ok(new DirectorEngagementMetricsDto
            {
                TotalProfileViews = totalViews,
                TotalInterests = totalInterests,
                TotalVideoWatches = 0, // or calculated value
                DailyActivity = dailyActivity.OrderBy(d => d.Date).ToList()
            });
        }

            /// <summary>
        /// Get staff performance
        /// </summary>
        [HttpGet("staff-performance")]
        public async Task<ActionResult<List<StaffPerformanceDto>>> GetStaffPerformance(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? sortBy,
            [FromQuery] string? sortDirection)
        {
            // 1. Get userId
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. Get bandId
            var bandId = await GetDirectorBandIdAsync(userId);
            if (bandId == null)
                return NotFound("Director band not found");

            var start = startDate ?? DateTime.UtcNow.AddDays(-30);
            var end = endDate ?? DateTime.UtcNow;

            var performance = await _directorService.GetStaffPerformanceAsync(bandId.Value, start, end);
            
            // Apply sorting if specified
            // ... sorting logic

            return Ok(performance);
        }

        /// <summary>
        /// Get pending approvals
        /// </summary>
        [HttpGet("pending-approvals")]
        public async Task<ActionResult<List<PendingApprovalDto>>> GetPendingApprovals()
        {
            // 1. Get userId
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. Get bandId
            var bandId = await GetDirectorBandIdAsync(userId);
            if (bandId == null)
                return NotFound("Director band not found");

            var approvals = await _directorService.GetPendingApprovalsAsync(bandId.Value);
            return Ok(approvals);
        }

        /// <summary>
        /// Approve scholarship offer
        /// </summary>
        [HttpPut("approvals/{approvalId}/approve")]
        public async Task<ActionResult> ApproveOffer(int approvalId, [FromBody] ApprovalDecisionDto decision)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Implementation would update approval status and notify staff
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving offer {ApprovalId}", approvalId);
                return StatusCode(500, "An error occurred while approving the offer");
            }
        }

        /// <summary>
        /// Deny scholarship offer
        /// </summary>
        [HttpPut("approvals/{approvalId}/deny")]
        public async Task<ActionResult> DenyOffer(int approvalId, [FromBody] ApprovalDecisionDto decision)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Implementation would update approval status and notify staff
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying offer {ApprovalId}", approvalId);
                return StatusCode(500, "An error occurred while denying the offer");
            }
        }

        /// <summary>
        /// Update staff budget
        /// </summary>
        [HttpPut("staff/{staffId}/budget")]
        public async Task<ActionResult> UpdateStaffBudget(int staffId, [FromBody] UpdateBudgetDto dto)
        {
            try
            {
                var staff = await _unitOfWork.BandStaff.GetByIdAsync(staffId);
                if (staff == null)
                    return NotFound();

                staff.BudgetAllocation = dto.Budget;
                await _unitOfWork.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating budget for staff {StaffId}", staffId);
                return StatusCode(500, "An error occurred while updating the budget");
            }
        }

        /// <summary>
        /// Get recent activity
        /// </summary>
        [HttpGet("activity")]
        public async Task<ActionResult<List<ActivityItemDto>>> GetActivity([FromQuery] int limit = 20)
        {
            // 1. Get userId
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            // 2. Get bandId
            var bandId = await GetDirectorBandIdAsync(userId);
            if (bandId == null)
                return NotFound("Director band not found");

            var activity = await _directorService.GetRecentActivityAsync(bandId.Value, limit);
            return Ok(activity);
        }

        /// <summary>
        /// Export dashboard
        /// </summary>
        [HttpGet("export")]
        public async Task<ActionResult> ExportDashboard(
            [FromQuery] string format,
            [FromQuery] bool includeCharts,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] string sections)
        {
            try
            {
                // Implementation would generate export file
                // Return appropriate file type based on format
                return File(new byte[0], "application/octet-stream", $"dashboard.{format}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting dashboard");
                return StatusCode(500, "An error occurred while exporting the dashboard");
            }
        }



        [HttpGet("band/{bandId}/scholarship-budget")]
        public async Task<ActionResult<BandBudgetDto>> GetScholarshipBudget(int bandId)
        {
            int currentYear = DateTime.UtcNow.Year;
            var budget = await _unitOfWork.BandBudgets.GetQueryable()
                .FirstOrDefaultAsync(b => b.BandId == bandId && b.FiscalYear == currentYear);

            if (budget == null) return NotFound("Budget not found for current fiscal year.");

            // Calculate pending amount (Sent but not yet Accepted/Declined)
            var pendingAmount = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.BandId == bandId &&
                           (o.Status == ScholarshipStatus.Sent || o.Status == ScholarshipStatus.PendingGuardianSignature))
                .SumAsync(o => o.ScholarshipAmount);

            return Ok(new BandBudgetDto
            {
                TotalBudget = budget.TotalBudget,
                Allocated = budget.AllocatedAmount,
                Remaining = budget.RemainingAmount,
                PendingCommitment = pendingAmount,
                FiscalYear = currentYear
            });
        }

        [HttpGet("band/{bandId}/conversion-funnel")]
        public async Task<ActionResult<ConversionFunnelDto>> GetConversionFunnel(int bandId)
        {
            var funnel = new ConversionFunnelDto
            {
                TotalInterests = await _unitOfWork.StudentInterests.GetQueryable().CountAsync(si => si.BandId == bandId),
                Contacted = await _unitOfWork.ContactLogs.GetQueryable().Select(c => c.StudentId).Distinct().CountAsync(), // Approximate count of distinct students contacted
                OffersSent = await _unitOfWork.ScholarshipOffers.GetQueryable().CountAsync(o => o.BandId == bandId),
                OffersAccepted = await _unitOfWork.ScholarshipOffers.GetQueryable().CountAsync(o => o.BandId == bandId && o.Status == ScholarshipStatus.Accepted)
            };

            return Ok(funnel);
        }

   

      

        /// <summary>
        /// Get staff member details
        /// </summary>
        [HttpGet("staff/{id}")]
        [ProducesResponseType(typeof(StaffDetailsDto), 200)]
        public async Task<ActionResult<StaffDetailsDto>> GetStaffMember(int id)
        {
            try
            {
                var staffDetails = await _directorService.GetStaffDetailsAsync(id);
                return Ok(staffDetails);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading staff details");
                return StatusCode(500, "An error occurred");
            }
        }

       

        /// <summary>
        /// Update staff permissions
        /// </summary>
        [HttpPut("staff/{staffId}/permissions")]
        public async Task<ActionResult> UpdateStaffPermissions(int staffId, [FromBody] BandStaffPermissionsDto permissions)
        {
            try
            {
                var updatedBy = User.FindFirst(ClaimTypes.Email)?.Value ?? "Unknown";

                await _directorService.UpdateStaffPermissionsAsync(staffId, permissions, updatedBy);

                return Ok(new { Message = "Permissions updated successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating permissions for staff {StaffId}", staffId);
                return StatusCode(500, "An error occurred while updating permissions");
            }
        }

        private async Task<int?> GetDirectorBandIdAsync(string userId)
        {
            var director = await _unitOfWork.BandStaff.GetQueryable()
                .Where(bs => bs.ApplicationUserId == userId && bs.Role == "Director")
                .Select(bs => bs.BandId)
                .FirstOrDefaultAsync();

            return director == 0 ? null : director;
        }

    }
}