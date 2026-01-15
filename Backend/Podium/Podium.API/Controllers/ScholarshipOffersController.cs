using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.API.Jobs;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.ScholarshipOffer;
using Podium.Application.Interfaces;
using Podium.Application.Services;
using Podium.Core.Constants;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using SendGrid.Helpers.Mail;
using System.Security.Claims;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScholarshipOffersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IScholarshipService _service;
        private readonly INotificationService _notificationService;
        private readonly IGuardianService _guardianService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        public ScholarshipOffersController(
            IUnitOfWork unitOfWork,
            IScholarshipService service,
            INotificationService notificationService,
            IGuardianService guardianService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _service = service;
            _notificationService = notificationService;
            _guardianService = guardianService;
            _backgroundJobClient = backgroundJobClient;
        }

        /// <summary>
        /// Create an offer
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "CanCreateOffer")]
        public async Task<ActionResult<ScholarshipOfferDto>> CreateOffer([FromBody] CreateScholarshipOfferDto dto)
        {
            // 1. Verify student exists to get Email
            var student = await _unitOfWork.Students.GetQueryable()
                .Include(s => s.ApplicationUser)
                .FirstOrDefaultAsync(s => s.Id == dto.StudentId);

            if (student == null) return NotFound("Student not found");

            var studentEmail = student.Email;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            // 2. Delegate Creation to Service
            var createdOffer = await _service.CreateOfferAsync(dto, userIdClaim, userRole == Roles.Director);

            // =========================================================
            // NOTIFICATION LOGIC
            // =========================================================

            // A. Real-time App Notification
            if (student.ApplicationUserId != null)
            {
                await _notificationService.NotifyUserAsync(
                    student.ApplicationUserId,
                    "ScholarshipOffer",
                    "You Received an Offer!",
                    $"You have received a {dto.OfferType} offer of ${dto.Amount:N0}!",
                    createdOffer.OfferId.ToString()
                );
            }

            // B. Email Background Jobs (Hangfire)
            if (!string.IsNullOrEmpty(studentEmail))
            {
                _backgroundJobClient.Enqueue<SendEmailNotificationsJob>(job =>
                    job.ExecuteAsync(studentEmail, "New Scholarship Offer", "You have received a new offer!"));

                var reminderDate = dto.ExpirationDate.AddDays(-1);
                if (reminderDate > DateTime.UtcNow)
                {
                    var delay = reminderDate - DateTime.UtcNow;
                    _backgroundJobClient.Schedule<SendEmailNotificationsJob>(job =>
                        job.ExecuteAsync(studentEmail, "Offer Expiring Soon", "Your offer expires tomorrow!"),
                        delay);
                }
            }

            // C. Notify Guardians
            var guardianUserIds = await _guardianService.GetGuardianUserIdsForStudentAsync(student.Id);
            foreach (var guardianId in guardianUserIds)
            {
                await _notificationService.NotifyUserAsync(
                    guardianId,
                    "ScholarshipOffer",
                    "New Offer for your Student",
                    $"{student.FirstName} has received a scholarship offer.",
                    createdOffer.OfferId.ToString()
                );
            }

            return Ok(new { Message = "Offer created and notifications queued", Offer = createdOffer });
        }

        /// <summary>
        /// Update offer status
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOfferStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var offer = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Include(o => o.Student)
                .Include(o => o.Band)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (offer == null) return NotFound();

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var isCreator = offer.CreatedByUserId == userIdClaim;
            var isDirector = role == Roles.Director;
            if (!isCreator && !isDirector && role != Roles.Student) return Forbid();

            // Update Status
            offer.Status = dto.ToScholarshipStatus();
            _unitOfWork.ScholarshipOffers.Update(offer);
            await _unitOfWork.SaveChangesAsync();

            // Notify Band Staff if Accepted/Declined
            if (offer.Status == ScholarshipStatus.Accepted || offer.Status == ScholarshipStatus.Declined)
            {
                var studentName = $"{offer.Student?.FirstName} {offer.Student?.LastName}";
                await _notificationService.NotifyBandStaffAsync(
                    offer.BandId,
                    "OfferUpdate",
                    $"Offer {dto.Status}",
                    $"{studentName} has {dto.Status.ToLower()} the scholarship offer.",
                    offer.Id.ToString()
                );
            }

            return Ok(new { Message = "Offer status updated successfully" });
        }

        [HttpGet]
        [Authorize(Policy = "DirectorOnly")]
        public async Task<ActionResult<ScholarshipOverviewDto>> GetAll([FromQuery] ScholarshipFilterDto filters)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            try { return Ok(await _service.GetScholarshipsAsync(userId, filters)); }
            catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScholarshipOfferDto>> GetOffer(int id)
        {
            var offer = await _service.GetOfferByIdAsync(id);
            if (offer == null) return NotFound();

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == Roles.Student)
            {
                var student = await _unitOfWork.Students.FirstOrDefaultAsync(s => s.ApplicationUserId == userIdClaim && s.Id == offer.StudentId);
                if (student == null) return Forbid();
            }
            // Refactored: Use Roles constants
            else if (role != Roles.BandStaff && role != Roles.Director)
            {
                return Forbid();
            }

            return Ok(offer);
        }

        [HttpPost("{id}/approve")]
        [Authorize(Policy = "CanApproveScholarships")]
        public async Task<IActionResult> ApproveScholarship(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            try
            {
                await _service.ApproveOfferAsync(id, userId);
                return Ok(new { Message = "Offer Approved and Sent" });
            }
            catch (Exception ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}/respond")]
        public async Task<IActionResult> Respond(int id, [FromBody] RespondToScholarshipOfferDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            bool isGuardian = User.HasClaim(c => c.Type == "IsGuardian" && c.Value == "true");

            try
            {
                await _service.RespondToOfferAsync(id, dto, userId, isGuardian);
                return Ok(new { Message = $"Offer {(dto.Accept ? "Accepted" : "Declined")}" });
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpPut("{id}/rescind")]
        [Authorize(Policy = "CanApproveScholarships")]
        public async Task<IActionResult> Rescind(int id, [FromBody] RescindScholarshipRequest dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _service.RescindOfferAsync(id, dto, userId);
                return Ok(new { Message = "Offer Rescinded" });
            }
            catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
        }

        [HttpGet("band/{bandId}/budget")]
        [Authorize(Policy = "DirectorOnly")]
        public async Task<ActionResult<ScholarshipBudgetDto>> GetBudget(int bandId)
        {
            var stats = await _service.GetBudgetStatsAsync(bandId);
            return Ok(stats);
        }

        [HttpGet("my-offers")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<IEnumerable<ScholarshipOfferDto>>> GetMyOffers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

            var student = await _unitOfWork.Students.GetQueryable()
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userIdClaim);

            if (student == null) return NotFound("Student profile not found");

            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.StudentId == student.Id)
                .Select(o => new ScholarshipOfferDto
                {
                    OfferId = o.Id,
                    StudentId = o.StudentId,
                    OfferType = o.OfferType,
                    Amount = o.ScholarshipAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    ApprovedAt = o.ApprovedDate
                })
                .ToListAsync();

            return Ok(offers);
        }

        [HttpGet("pending-scholarships")]
        [Authorize(Policy = "CanApproveScholarships")]
        public async Task<ActionResult<IEnumerable<ScholarshipOfferDto>>> GetPendingScholarships()
        {
            var offers = await _unitOfWork.ScholarshipOffers.GetQueryable()
                .Where(o => o.OfferType == "Scholarship" && o.Status == ScholarshipStatus.Pending)
                .Select(o => new ScholarshipOfferDto
                {
                    OfferId = o.Id,
                    StudentId = o.StudentId,
                    OfferType = o.OfferType,
                    Amount = o.ScholarshipAmount,
                    Status = o.Status,
                    Terms = o.Terms,
                    Notes = o.Description,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        [HttpGet("student/{studentId}/summaries")]
        public async Task<ActionResult<PagedResult<OfferSummaryDto>>> GetStudentOfferSummaries(
            int studentId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Optional: Add IsStudentOwnerAsync check

            var result = await _service.GetStudentOfferSummariesAsync(studentId, page, pageSize);

            if (result.IsSuccess) return Ok(result.Data);

            return result.ResultType switch
            {
                ServiceResultType.NotFound => NotFound(result.ErrorMessage),
                _ => BadRequest(result.ErrorMessage)
            };
        }

    }
}