namespace Podium.Application.DTOs.Guardian
{
    public class NotificationListDto
    {
        public List<NotificationDto> Notifications { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}