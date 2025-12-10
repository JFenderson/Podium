using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Offer;
using Podium.Application.Interfaces;
using Podium.Core.Interfaces;
using Podium.Core.Entities;
using Podium.Infrastructure.Data;
using System.Security.Claims;
using Podium.Core.Constants;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ScholarshipOffersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IScholarshipService _service;
        private readonly INotificationService _notificationService;
        private readonly IGuardianService _guardianService;
        private readonly IStudentService _studentService;

        public ScholarshipOffersController(ApplicationDbContext context, IScholarshipService service)
        {
            _context = context;
            _service = service;
        }

        /// <summary>
        /// Create an offer - Only Recruiters/Directors with CanSendOffers permission
        /// Scenario 2: Scholarship offer sent → notify student and guardians
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "CanCreateOffer")]
        public async Task<ActionResult<ScholarshipOfferDto>> CreateOffer([FromBody] CreateOfferDto dto)
        {
            // Verify student exists
            var student = await _context.Students
                .Include(s => s.ApplicationUser) // Include ApplicationUser to get UserId for notification
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId);

            if (student == null) return NotFound("Student not found");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                return Unauthorized();

            // Create Offer Entity
            var offer = new ScholarshipOffer
            {
                StudentId = dto.StudentId,
                BandId = dto.BandId, // Ensure this is in your DTO or derived from Staff context
                CreatedByUserId = userId.ToString(),
                OfferType = dto.OfferType,
                ScholarshipAmount = dto.ScholarshipAmount,
                Description = dto.Description,
                Status = ScholarshipStatus.PendingApproval,
                CreatedAt = DateTime.UtcNow
            };

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            // =========================================================
            // NOTIFICATION LOGIC (Scenario 2)
            // =========================================================

            // 1. Notify the Student
            if (student.ApplicationUserId != null)
            {
                await _notificationService.NotifyUserAsync(
                    student.ApplicationUserId,
                    "ScholarshipOffer",
                    "You Received an Offer!",
                    $"You have received a {dto.OfferType} offer of ${dto.ScholarshipAmount:N0}!",
                    offer.OfferId.ToString()
                );
            }

            // 2. Notify Guardians (Optional: Check preferences first if implemented)
            // Assuming IGuardianService has a method to get Guardian UserIds
            // If not, you can implement a helper here using _context.StudentGuardians
            var guardianUserIds = await _guardianService.GetGuardianUserIdsForStudentAsync(student.StudentId);

            foreach (var guardianId in guardianUserIds)
            {
                await _notificationService.NotifyUserAsync(
                    guardianId,
                    "ScholarshipOffer",
                    "New Offer for your Student",
                    $"{student.FirstName} has received a scholarship offer.",
                    offer.OfferId.ToString()
                );
            }
            // =========================================================

            return CreatedAtAction(nameof(GetOffer), new { id = offer.OfferId }, new ScholarshipOfferDto
            {
                OfferId = offer.OfferId,
                StudentId = offer.StudentId,
                OfferType = offer.OfferType,
                ScholarshipAmount = offer.ScholarshipAmount,
                Status = offer.Status,
                CreatedAt = offer.CreatedAt
            });
        }

        /// <summary>
        /// Update offer status
        /// Scenario 3: Scholarship offer accepted/declined → notify all band staff
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOfferStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var offer = await _context.Offers
                .Include(o => o.Student) // Include for names in notification
                .Include(o => o.Band)    // Include for BandId
                .FirstOrDefaultAsync(o => o.OfferId == id);

            if (offer == null) return NotFound();

            // ... (Your existing authorization checks from OffersController.cs) ...
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId)) return Unauthorized();

            var isCreator = offer.CreatedByUserId == userId.ToString();
            var isDirector = role == "Director";
            if (!isCreator && !isDirector && role != "Student") return Forbid(); // Allow students to update too?

            // Update Status
            offer.Status = dto.ToScholarshipStatus();
            await _context.SaveChangesAsync();

            // =========================================================
            // NOTIFICATION LOGIC (Scenario 3)
            // =========================================================
            if (offer.Status == ScholarshipStatus.Accepted || offer.Status == ScholarshipStatus.Declined)
            {
                var studentName = $"{offer.Student?.FirstName} {offer.Student?.LastName}";

                await _notificationService.NotifyBandStaffAsync(
                    offer.BandId,
                    "OfferUpdate",
                    $"Offer {dto.Status}",
                    $"{studentName} has {dto.Status.ToLower()} the scholarship offer.",
                    offer.OfferId.ToString()
                );
            }

            return Ok(new { Message = "Offer status updated successfully" });
        }


        [HttpGet] // GET /api/scholarships
        [Authorize(Policy = "DirectorOnly")] // or "CanViewScholarships"
        public async Task<ActionResult<ScholarshipOverviewDto>> GetAll([FromQuery] ScholarshipFilterDto filters)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var result = await _service.GetScholarshipsAsync(userId, filters);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        /// <summary>
        /// Get offer by ID - Students can view their offers, BandStaff with CanViewStudents can view all
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ScholarshipOfferDto>> GetOffer(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Students can only view their own offers
            if (role == "Student")
            {
                var student = await _context.Students
                    .FirstOrDefaultAsync(s => s.ApplicationUserId == userIdClaim && s.StudentId == offer.StudentId);

                if (student == null)
                {
                    return Forbid();
                }
            }
            // BandStaff must have ViewStudents permission (checked by policy in more secure endpoints)
            else if (role != "Recruiter" && role != "Director")
            {
                return Forbid();
            }

            return Ok();
        }

        /// <summary>
        /// Approve scholarship offer - Only Directors can approve
        /// </summary>
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
        public async Task<IActionResult> Respond(int id, [FromBody] RespondToOfferDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if user is Guardian or Student
            // In a real app, you might check a Permission Service here
            bool isGuardian = User.HasClaim(c => c.Type == "IsGuardian" && c.Value == "true");

            try
            {
                await _service.RespondToOfferAsync(id, dto, userId, isGuardian);
                return Ok(new { Message = $"Offer {(dto.Accept ? "Accepted" : "Declined")}" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message); // "Expired" or "Wrong State"
            }
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("band/{bandId}/budget")]
        [Authorize(Policy = "DirectorOnly")]
        public async Task<ActionResult<ScholarshipBudgetDto>> GetBudget(int bandId)
        {
            var stats = await _service.GetBudgetStatsAsync(bandId);
            return Ok(stats);
        }

        /// <summary>
        /// Get all offers for current student
        /// </summary>
        [HttpGet("my-offers")]
        [Authorize(Policy = "StudentOnly")]
        public async Task<ActionResult<IEnumerable<ScholarshipOfferDto>>> GetMyOffers()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userIdClaim);

            if (student == null)
            {
                return NotFound("Student profile not found");
            }

            var offers = await _context.Offers
                .Where(o => o.StudentId == student.StudentId)
                .Select(o => new ScholarshipOfferDto
                {
                    OfferId = o.OfferId,
                    StudentId = o.StudentId,
                    OfferType = o.OfferType,
                    ScholarshipAmount = o.ScholarshipAmount,
                    Status = o.Status,
                    CreatedAt = o.CreatedAt,
                    ApprovedAt = o.ApprovedDate
                })
                .ToListAsync();

            return Ok(offers);
        }

        /// <summary>
        /// Get pending scholarship offers - Only Directors can view
        /// </summary>
        [HttpGet("pending-scholarships")]
        [Authorize(Policy = "CanApproveScholarships")]
        public async Task<ActionResult<IEnumerable<ScholarshipOfferDto>>> GetPendingScholarships()
        {
            var offers = await _context.Offers
                .Where(o => o.OfferType == "Scholarship" && o.Status == ScholarshipStatus.PendingApproval)
                .Select(o => new ScholarshipOfferDto
                {
                    OfferId = o.OfferId,
                    StudentId = o.StudentId,
                    OfferType = o.OfferType,
                    ScholarshipAmount = o.ScholarshipAmount,
                    Status = o.Status,
                    Terms = o.Terms,
                    Notes = o.Description,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        /// <summary>
        /// Update offer status - Using custom authorization logic
        /// </summary>
        //[HttpPut("{id}/status")]
        //public async Task<IActionResult> UpdateOfferStatus(int id, [FromBody] UpdateStatusDto dto)
        //{
        //    var offer = await _context.Offers.FindAsync(id);
        //    if (offer == null)
        //    {
        //        return NotFound();
        //    }

        //    var role = User.FindFirst(ClaimTypes.Role)?.Value;
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
        //    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        //    {
        //        return Unauthorized();
        //    }

        //    // Only the creator or a Director can update status
        //    var isCreator = offer.CreatedByStaffId == userId;
        //    var isDirector = role == "Director";

        //    if (!isCreator && !isDirector)
        //    {
        //        return Forbid();
        //    }

        //    // Directors can set any status, others can only withdraw
        //    if (!isDirector && dto.Status != "Withdrawn")
        //    {
        //        return Forbid();
        //    }

        //    offer.Status = dto.ToScholarshipStatus();
        //    await _context.SaveChangesAsync();

        //    return Ok(new { Message = "Offer status updated successfully" });
        //}
    }
}
