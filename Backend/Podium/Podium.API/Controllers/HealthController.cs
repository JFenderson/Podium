using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Podium.Infrastructure.Data;

namespace Podium.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ApplicationDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Basic health check endpoint - returns 200 OK if application is running
    /// </summary>
    [HttpGet]
    [Route("/health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            service = "Podium API"
        });
    }

    /// <summary>
    /// Readiness check endpoint - returns 200 OK if application is ready to serve traffic
    /// Checks database connectivity and other dependencies
    /// </summary>
    [HttpGet]
    [Route("/ready")]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();

            return Ok(new
            {
                status = "Ready",
                timestamp = DateTime.UtcNow,
                service = "Podium API",
                checks = new
                {
                    database = "Connected"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");

            return StatusCode(503, new
            {
                status = "NotReady",
                timestamp = DateTime.UtcNow,
                service = "Podium API",
                checks = new
                {
                    database = "Disconnected"
                },
                error = ex.Message
            });
        }
    }
}
