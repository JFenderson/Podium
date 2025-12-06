namespace Podium.Application.DTOs.Director
{
    public class StaffActivitySummaryDto
    {
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int ContactsInitiated { get; set; }
        public int OffersCreated { get; set; }
        public DateTime? LastActiveDate { get; set; }
    }
}