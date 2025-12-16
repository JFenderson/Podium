using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Podium.Core.Interfaces;
using System.Security.Claims;

namespace Podium.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        /// <summary>
        /// Get recent notifications for the current user
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetRecent([FromQuery] int count = 20)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var notifications = await _notificationService.GetRecentNotificationsAsync(userId, count);
            return Ok(notifications);
        }

        /// <summary>
        /// Get the count of unread notifications
        /// </summary>
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { Count = count });
        }

        /// <summary>
        /// Mark a specific notification as read
        /// </summary>
        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            // Optional: You might want to verify the notification belongs to the user 
            // inside the service or here before marking it read.
            await _notificationService.MarkAsReadAsync(id);
            return NoContent();
        }
    }
}