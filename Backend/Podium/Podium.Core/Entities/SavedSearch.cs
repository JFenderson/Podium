using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class SavedSearch
    {
        public int Id { get; set; }
        public int BandStaffId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // JSON serialized search criteria
        public string FilterCriteria { get; set; } = "{}";

        // Alert settings
        public bool AlertsEnabled { get; set; }
        public int? AlertFrequencyDays { get; set; } // null = instant, 1 = daily, 7 = weekly
        public DateTime? LastAlertSent { get; set; }
        public int LastResultCount { get; set; }

        // Sharing
        public bool IsShared { get; set; }
        public string? ShareToken { get; set; } // URL-safe token for sharing

        // Quick filter template
        public bool IsTemplate { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? LastUsed { get; set; }
        public int TimesUsed { get; set; }

        // Navigation
        public BandStaff BandStaff { get; set; } = null!;
    }
}
