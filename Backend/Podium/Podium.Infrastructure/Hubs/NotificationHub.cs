using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Podium.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace Podium.Infrastructure.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public NotificationHub(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. Connection Lifecycle
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }

        // 2. Group Management
        public async Task JoinBandGroup(int bandId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Band_{bandId}");
        }

        public async Task LeaveBandGroup(int bandId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Band_{bandId}");
        }

        // 3. Client Actions
        //public async Task MarkAsRead(int notificationId)
        //{
        //    var userId = Context.UserIdentifier;
        //    var notification = await _context.Notifications
        //        .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        //    if (notification != null)
        //    {
        //        notification.IsRead = true;
        //        await _context.SaveChangesAsync();

        //        // Notify the client to update their badge count immediately
        //        await Clients.User(userId).SendAsync("UpdateUnreadCount", -1); // Decrement locally
        //    }
        //}
    }
}