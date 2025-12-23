namespace Podium.Application.DTOs.Student
{
    public class StudentActivityDto
    {
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Icon { get; set; } = "info"; // Default icon mapping
    }
}