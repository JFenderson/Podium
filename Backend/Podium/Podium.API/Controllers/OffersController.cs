using Podium.Core.Entities;
using Podium.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Offer;
using System.Security.Claims;

namespace Podium.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OffersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public OffersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Create an offer - Only Recruiters/Directors with CanSendOffers permission
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "CanCreateOffer")]
        public async Task<ActionResult<ScholarshipOfferDto>> CreateOffer([FromBody] CreateOfferDto dto)
        {
            // Verify student exists
            var student = await _context.Students.FindAsync(dto.StudentId);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            var offer = new Offer
            {
                StudentId = dto.StudentId,
                CreatedByUserId = userId,
                OfferType = dto.OfferType,
                ScholarshipAmount = dto.ScholarshipAmount,
                Description = dto.Description,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetOffer),
                new { id = offer.OfferId },
                new ScholarshipOfferDto
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

            return Ok(new ScholarshipOfferDto
            {
                OfferId = offer.OfferId,
                StudentId = offer.StudentId,
                OfferType = offer.OfferType,
                ScholarshipAmount = offer.ScholarshipAmount,
                Status = offer.Status,
                CreatedAt = offer.CreatedAt,
                ApprovedDate = offer.ApprovedAt
            });
        }

        /// <summary>
        /// Approve scholarship offer - Only Directors can approve
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Policy = "CanApproveScholarships")]
        public async Task<IActionResult> ApproveScholarship(int id)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            if (offer.OfferType != "Scholarship")
            {
                return BadRequest("Only scholarship offers can be approved through this endpoint");
            }

            if (offer.Status == "Approved")
            {
                return BadRequest("Offer is already approved");
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;  // ✅ Keep as string
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            offer.Status = "Approved";
            offer.ApprovedByUserId = userId;  // ✅ Assign string directly
            offer.ApprovedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Scholarship offer approved successfully" });
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
                    ApprovedDate = o.ApprovedAt
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
                .Where(o => o.OfferType == "Scholarship" && o.Status == "Pending")
                .Select(o => new ScholarshipOfferDto
                {
                    OfferId = o.OfferId,
                    StudentId = o.StudentId,
                    OfferType = o.OfferType,
                    ScholarshipAmount = o.ScholarshipAmount,
                    Status = o.Status,
                    Terms = o.Terms,
                    Notes = o.Notes,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync();

            return Ok(offers);
        }

        /// <summary>
        /// Update offer status - Using custom authorization logic
        /// </summary>
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOfferStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var offer = await _context.Offers.FindAsync(id);
            if (offer == null)
            {
                return NotFound();
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized();
            }

            // Only the creator or a Director can update status
            var isCreator = offer.CreatedByUserId == userId;
            var isDirector = role == "Director";

            if (!isCreator && !isDirector)
            {
                return Forbid();
            }

            // Directors can set any status, others can only withdraw
            if (!isDirector && dto.Status != "Withdrawn")
            {
                return Forbid();
            }

            offer.Status = dto.Status;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Offer status updated successfully" });
        }
    }
}
