namespace Podium.Application.DTOs.Band
{
    public class DemographicBreakdownDto
    {
        public Dictionary<int, int> ByGraduationYear { get; set; } = new();
        public Dictionary<string, int> ByInstrument { get; set; } = new();
        public Dictionary<string, int> BySkillLevel { get; set; } = new();
        public Dictionary<string, int> ByState { get; set; } = new();
        public Dictionary<string, int> BySchoolType { get; set; } = new(); // Public, Private, Magnet, etc.
    }
}