namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Time series data point
    /// </summary>
    public class PerformanceTimeSeriesDto
    {
        public string Month { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public int OffersCreated { get; set; }
        public int OffersAccepted { get; set; }
        public int ContactsMade { get; set; }
        public int ResponsesReceived { get; set; }
    }
}