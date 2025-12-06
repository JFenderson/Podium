using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.Band
{
    /// <summary>
    /// Detailed analytics for a band with trend data and insights.
    /// Includes complex aggregations over time periods.
    /// </summary>
    public class BandAnalyticsDto
    {
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Interest trends
        public List<MonthlyTrendDto> InterestTrend { get; set; } = new();
        
        // Scholarship metrics
        public decimal TotalScholarshipOffered { get; set; }
        public decimal TotalScholarshipAccepted { get; set; }
        public decimal AverageOfferAmount { get; set; }
        public double OfferAcceptanceRate { get; set; }
        
        // Distribution analytics
        public List<InstrumentDistributionDto> InstrumentDistribution { get; set; } = new();
        public List<GeographicDistributionDto> GeographicDistribution { get; set; } = new();
        
        // Additional metrics
        public InterestTrendsDto? InterestTrends { get; set; }
        public ScholarshipAnalyticsDto? ScholarshipAnalytics { get; set; }
        public EngagementMetricsDto? EngagementMetrics { get; set; }
        public ConversionMetricsDto? ConversionMetrics { get; set; }
        public DemographicBreakdownDto? Demographics { get; set; }
    }
}
