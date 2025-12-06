namespace Podium.Application.DTOs.Guardian
{
    public class StudentSummaryDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string PrimaryInstrument { get; set; } = string.Empty;
        public int GraduationYear { get; set; }

        // Quick stats
        public int PendingContactRequests { get; set; }
        public int ActiveScholarshipOffers { get; set; }
        public int BandsInterested { get; set; }
        public DateTime? LastActivityDate { get; set; }

        // Alerts
        public bool HasExpiringOffers { get; set; }
        public bool HasUrgentApprovals { get; set; }
    }
}