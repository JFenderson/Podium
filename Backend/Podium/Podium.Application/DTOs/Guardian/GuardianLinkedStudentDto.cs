namespace Podium.Application.DTOs.Guardian
{
    public class GuardianLinkedStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string PrimaryInstrument { get; set; } = string.Empty;
        public int GraduationYear { get; set; }

        // Stats
        public int PendingContactRequests { get; set; }
        public int ActiveScholarshipOffers { get; set; }
        public int BandsInterested { get; set; }
        public DateTime LastActivityDate { get; set; }

        // UI Flags
        public bool HasExpiringOffers { get; set; }
        public bool HasUrgentApprovals { get; set; }
    }
}