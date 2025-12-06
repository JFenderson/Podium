using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Guardian notification preferences with granular control.
    /// </summary>
    public class GuardianNotificationPreferencesDto
    {
        public string UserId { get; set; } = string.Empty;

        // Channel preferences
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public bool InAppEnabled { get; set; } = true;

        // Event preferences
        public bool NotifyOnNewOffer { get; set; } = true;
        public bool NotifyOnContactRequest { get; set; } = true;
        public bool NotifyOnOfferExpiring { get; set; } = true;
        public int OfferExpiringDaysThreshold { get; set; } = 7; // Notify when offer expires within X days
        public bool NotifyOnVideoUpload { get; set; } = true;
        public bool NotifyOnInterestShown { get; set; } = false;
        public bool NotifyOnEventRegistration { get; set; } = true;

        // Frequency settings
        public string DigestFrequency { get; set; } = "Daily"; // Immediate, Daily, Weekly, None
        public TimeSpan? QuietHoursStart { get; set; } // No notifications during these hours
        public TimeSpan? QuietHoursEnd { get; set; }

        // Per-student customization (overrides)
        public Dictionary<int, StudentNotificationOverrideDto>? StudentOverrides { get; set; }

        public DateTime LastUpdated { get; set; }
    }

}
