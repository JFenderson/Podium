namespace Podium.Application.DTOs.Band
{
    public class InterestTrendsDto
    {
        public int TotalInterested { get; set; }
        public decimal PercentageChange { get; set; } // vs previous period
        public List<MonthlyInterestDto> MonthlyBreakdown { get; set; } = new();
        public Dictionary<string, int> InterestByInstrument { get; set; } = new();
        public Dictionary<string, int> InterestBySkillLevel { get; set; } = new();
    }
}