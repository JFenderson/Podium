namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Pending task for this staff member
    /// </summary>
    public class MyPendingTaskDto
    {
        public int Id { get; set; }
        public string TaskType { get; set; } = string.Empty; // FollowUp, ResponseNeeded, OfferExpiring
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public string Priority { get; set; } = string.Empty; // High, Medium, Low
        public int? StudentId { get; set; }
        public string? StudentName { get; set; }
        public bool CanComplete { get; set; }
    }
}