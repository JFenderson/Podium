namespace Podium.Application.DTOs.Student
{
    public class StudentDashboardDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string PrimaryInstrument { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string GuardianInviteCode { get; set; } = string.Empty;

        // Stats
        public int TotalProfileViews { get; set; }
        public int SearchAppearances { get; set; }
        public int ActiveOffers { get; set; }
        public int PendingContactRequests { get; set; }

        // Recent items
        public List<StudentNotificationDto> RecentNotifications { get; set; } = new();
        public List<StudentActivityDto> RecentActivity { get; set; } = new();
    }
}