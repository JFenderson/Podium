namespace Podium.Application.DTOs.Guardian
{
    public class PriorityAlertDto
    {
        public string AlertType { get; set; } = string.Empty; // "ExpiringOffer", "UrgentApproval", "ImportantActivity"
        public string Message { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public string ActionUrl { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // "High", "Medium", "Low"
    }
}