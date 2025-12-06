using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        // Student Interest Trends
        public InterestTrendsDto InterestTrends { get; set; } = new();

        // Scholarship Analytics
        public ScholarshipAnalyticsDto ScholarshipAnalytics { get; set; } = new();

        // Conversion Metrics
        public ConversionMetricsDto ConversionMetrics { get; set; } = new();

        // Demographic Breakdown
        public DemographicBreakdownDto Demographics { get; set; } = new();

        // Engagement Metrics
        public EngagementMetricsDto Engagement { get; set; } = new();

        // Geographic Distribution
        public List<GeographicDataDto> GeographicDistribution { get; set; } = new();
    }
}
