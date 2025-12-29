namespace Podium.Application.DTOs.Director
{
    public class DirectorKeyMetricsDto
    {
        // Offers
        public int TotalOffersSent { get; set; }
        public double OffersSentChange { get; set; }

        // Acceptance
        public double AcceptanceRate { get; set; }
        public double AcceptanceRateChange { get; set; }

        // BandStaff
        public int ActiveBandStaff { get; set; }
        public double ActiveBandStaffChange { get; set; }

        // Pipeline
        public int PipelineStudents { get; set; }
        public double PipelineStudentsChange { get; set; }

        // Budget
        public decimal TotalBudgetAllocated { get; set; }
        public decimal TotalBudgetUsed { get; set; }
        public double BudgetUtilization { get; set; }

        // Averages
        public decimal AverageOfferAmount { get; set; }
        public List<string>? TopInstrumentNeeds { get; set; }
    }
}