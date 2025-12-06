using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.DTOs.Offer;

namespace Podium.Application.DTOs.Director
{
    /// <summary>
    /// Comprehensive dashboard view for band directors.
    /// Optimized to load in a single query with strategic projections.
    /// </summary>
    public class DirectorDashboardDto
    {
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;

        // Student Interest Metrics
        public int TotalInterestedStudents { get; set; }
        public int NewInterestedLastWeek { get; set; }
        public int NewInterestedLastMonth { get; set; }

        // Scholarship Overview
        public ScholarshipSummaryDto ScholarshipSummary { get; set; } = new();

        // Upcoming Events
        public List<UpcomingEventDto> UpcomingEvents { get; set; } = new();

        // Staff Activity
        public List<StaffActivitySummaryDto> StaffActivity { get; set; } = new();

        // Recent Activity Feed
        public List<RecentActivityDto> RecentActivities { get; set; } = new();

        // Quick Stats
        public int TotalVideosSubmitted { get; set; }
        public int VideosAwaitingReview { get; set; }
        public int PendingContactRequests { get; set; }
    }
}
