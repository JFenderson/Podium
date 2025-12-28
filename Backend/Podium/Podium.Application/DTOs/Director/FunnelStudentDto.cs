namespace Podium.Application.DTOs.Director
{
    public class FunnelStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string Instrument { get; set; }
        public string CurrentStage { get; set; }
        public int DaysInStage { get; set; }
        public double? GPA { get; set; }
        public int GraduationYear { get; set; }
    }
}