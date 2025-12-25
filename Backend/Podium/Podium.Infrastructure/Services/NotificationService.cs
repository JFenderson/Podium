using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Podium.Core.Constants;
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

        public async Task NotifyUserAsync(string userId, string type, string title, string message,
             string? relatedId = null,
             NotificationPriority priority = NotificationPriority.Low,
             DateTime? expiresAt = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Type = type,
                Title = title,
                Message = message,
                RelatedEntityId = relatedId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false,
                Priority = priority,   // Set Priority
                ExpiresAt = expiresAt  // Set Expiration
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.SaveChangesAsync();

            // Send Full Object via SignalR including Priority
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", notification);
            await _hubContext.Clients.User(userId).SendAsync("UpdateUnreadCount", 1);
        }

        // Updated for Band Staff
        public async Task NotifyBandStaffAsync(int bandId, string type, string title, string message,
            string? relatedId = null,
            NotificationPriority priority = NotificationPriority.Low)
        {
            var staffMembers = await _unitOfWork.BandStaff
                .FindAsync(b => b.BandId == bandId && b.IsActive && b.CanViewStudents);

            var userIds = staffMembers.Select(s => s.ApplicationUserId).Distinct().ToList();
            var notifications = new List<Notification>();

            foreach (var staff in staffMembers)
            {
                notifications.Add(new Notification
                {
                    UserId = staff.ApplicationUserId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityId = relatedId,
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    Priority = priority // Apply to all staff
                });
            }

            if (notifications.Any())
            {
                await _unitOfWork.Notifications.AddRangeAsync(notifications);
                await _unitOfWork.SaveChangesAsync();
            }

            if (userIds.Any())
            {
                // Send simplified object via SignalR
                await _hubContext.Clients.Users(userIds).SendAsync("ReceiveNotification", new
                {
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedEntityId = relatedId,
                    Priority = priority,
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