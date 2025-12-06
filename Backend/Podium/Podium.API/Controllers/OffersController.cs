using Podium.Infrastructure.Authorization;
using Podium.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Student;
using Podium.Application.DTOs.Offer;
using Podium.Application.DTOs.BandStaff;
using Podium.Application.DTOs.Rating;

namespace Podium.API.Controllers
{

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OffersController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IAuthorizationService _authService;

    public OffersController(
        ApplicationDbContext context,
        IAuthorizationService authService)
    {
        _context = context;
        _authService = authService;
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

        var userId = await _authService.GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var offer = new Offer
        {
            StudentId = dto.StudentId,
            CreatedByUserId = userId.Value,
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
                CreatedDate = offer.CreatedAt
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

        var userId = await _authService.GetCurrentUserIdAsync();
        var role = await _authService.GetCurrentUserRoleAsync();

        // Students can only view their own offers
        if (role == Roles.Student)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.ApplicationUserId == userId && s.StudentId == offer.StudentId);

            if (student == null)
            {
                return Forbid();
            }
        }
        // BandStaff must have ViewStudents permission
        else if (role == Roles.Recruiter || role == Roles.Director)
        {
            if (!await _authService.HasPermissionAsync(Permissions.ViewStudents))
            {
                return Forbid();
            }
        }
        else
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
            CreatedDate = offer.CreatedAt,
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

        var userId = await _authService.GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        offer.Status = "Approved";
        offer.ApprovedByUserId = userId.Value;
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
        var userId = await _authService.GetCurrentUserIdAsync();
        if (userId == null)
        {
            return Unauthorized();
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.ApplicationUserId == userId.Value);

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
                CreatedDate = o.CreatedAt,
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
                Description = o.Description,
                CreatedDate = o.CreatedAt
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

        var role = await _authService.GetCurrentUserRoleAsync();

        // Only the creator or a Director can update status
        var userId = await _authService.GetCurrentUserIdAsync();
        var isCreator = offer.CreatedByUserId == userId;
        var isDirector = role == Roles.Director;

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
