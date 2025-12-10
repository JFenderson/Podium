using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Application.DTOs.Auth;
using Podium.Application.Interfaces;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Data;

namespace Podium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;
    private readonly ApplicationDbContext _context;

    public AuthController(IAuthService authService, ILogger<AuthController> logger, ApplicationDbContext context)
    {
        _authService = authService;
        _logger = logger;
        _context = context;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
        {
            return BadRequest(new { errors = result.Errors });
        }

        return Ok(result);
    }

    // Helper for Frontend Registration Form
    [HttpGet("registration-options")]
    public async Task<IActionResult> GetRegistrationOptions()
    {
        var bands = await _context.Bands
            .Where(b => b.IsActive)
            .Select(b => new { b.BandId, b.BandName, b.State })
            .ToListAsync();

        var roles = new[] { "Student", "Guardian", "Recruiter", "Director" };

        return Ok(new { Bands = bands, Roles = roles });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.LoginAsync(dto);

        if (!result.Success)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt,
            userId = result.UserId,
            email = result.Email
        });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var result = await _authService.RefreshTokenAsync(dto);

        if (!result.Success)
        {
            return Unauthorized(new { errors = result.Errors });
        }

        return Ok(new
        {
            accessToken = result.AccessToken,
            refreshToken = result.RefreshToken,
            expiresAt = result.ExpiresAt
        });
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.RefreshToken))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var result = await _authService.RevokeTokenAsync(dto);

        if (!result)
        {
            return BadRequest(new { error = "Failed to revoke token" });
        }

        return Ok(new { message = "Logged out successfully" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _authService.GetUserByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            firstName = user.FirstName,
            lastName = user.LastName,
            createdAt = user.CreatedAt
        });
    }
}

