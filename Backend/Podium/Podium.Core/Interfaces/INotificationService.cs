using Podium.Core.Constants;
using Podium.Core.Entities;

namespace Podium.Core.Interfaces
{
    public interface INotificationService
    {
        Task NotifyUserAsync(string userId, string type, string title, string message, string? relatedId = null, NotificationPriority priority = NotificationPriority.Low, DateTime? expiresAt = null);
        Task NotifyBandStaffAsync(int bandId, string type, string title, string message, string? relatedId = null, NotificationPriority priority = NotificationPriority.Low);
        Task MarkAsReadAsync(int notificationId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<Notification>> GetRecentNotificationsAsync(string userId, int count = 20);
    }
}