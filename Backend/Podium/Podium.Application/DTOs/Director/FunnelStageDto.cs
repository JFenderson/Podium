namespace Podium.Application.DTOs.Director
{
    public class FunnelStageDto
    {
        public string Stage { get; set; } // Contacted, Interested, Offered, Accepted, Enrolled
        public int Count { get; set; }
        public double Percentage { get; set; }
        public double? ConversionRate { get; set; }
        public List<FunnelStudentDto>? Students { get; set; }
    }
}