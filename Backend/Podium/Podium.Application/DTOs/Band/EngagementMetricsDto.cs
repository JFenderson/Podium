namespace Podium.Application.DTOs.Band
{
    public class EngagementMetricsDto
    {
        public int TotalVideosUploaded { get; set; }
        public double AverageVideosPerStudent { get; set; }
        public int EventRegistrations { get; set; }
        public int EventAttendances { get; set; }
        public double EventAttendanceRate { get; set; }
        public int ProfileViews { get; set; }
        public double AverageResponseTimeHours { get; set; } // Staff response time to inquiries
    }
}