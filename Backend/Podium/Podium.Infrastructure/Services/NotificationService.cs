using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
using Podium.Infrastructure.Hubs;

namespace Podium.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IHubContext<NotificationHub> hubContext, IUnitOfWork unitOfWork)
        {
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
        }

        public async Task NotifyUserAsync(string userId, string type, string title, string message, string? relatedId = null)
        {
            // 1. Persist to Database
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityId = relatedId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // 2. Send Real-time via SignalR
            // We send the full object so the frontend can update the UI immediately
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);

            // Optional: Update unread count badge immediately
            await _hubContext.Clients.User(userId).SendAsync("UpdateUnreadCount", 1); // Increment logic on client
        }

        public async Task NotifyBandStaffAsync(int bandId, string type, string title, string message, string? relatedId = null)
        {
            // 1. Find all staff members to save to DB
            // Note: Ensure your BandStaff repository supports inclusion or direct querying
            var staffMembers = await _unitOfWork.BandStaff
                .FindAsync(b => b.BandId == bandId && b.IsActive && b.CanViewStudents);

            var userIds = staffMembers.Select(s => s.ApplicationUserId).Distinct().ToList();
            var notifications = new List<Notification>();

            foreach (var staff in staffMembers)
            {
                var notification = new Notification
                {
                    UserId = staff.ApplicationUserId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityId = relatedId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false
                };
                await _unitOfWork.Notifications.AddAsync(notification);
            }

            if (notifications.Any())
            {
                await _unitOfWork.Notifications.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();
            }

            if (userIds.Any())
            {
                await _hubContext.Clients.Users(userIds).SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityId = relatedId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                _unitOfWork.Notifications.Update(notification);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            // Using the Generic Repository's CountAsync if available, otherwise utilizing Find
            // Assuming your IRepository has CountAsync(Expression<Func<T, bool>>)
            return await _unitOfWork.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 20)
        {
            // Optimized: Use GetQueryable to sort and take on the database side
            return await _unitOfWork.Notifications.GetQueryable()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        // Implement GetUnreadCountAsync, MarkAsReadAsync, etc. using _unitOfWork
    }
}