using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class UpdatePreferencesRequest
    {
        // Channel preferences
        public bool? EmailEnabled { get; set; }
        public bool? SmsEnabled { get; set; }
        public bool? InAppEnabled { get; set; }

        // Event preferences
        public bool? NotifyOnNewOffer { get; set; }
        public bool? NotifyOnContactRequest { get; set; }
        public bool? NotifyOnOfferExpiring { get; set; }
        public int? OfferExpiringDaysThreshold { get; set; }
        public bool? NotifyOnVideoUpload { get; set; }
        public bool? NotifyOnInterestShown { get; set; }
        public bool? NotifyOnEventRegistration { get; set; }

        // Frequency settings
        public string? DigestFrequency { get; set; }
        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }

        // Per-student overrides
        public Dictionary<int, StudentNotificationOverrideDto>? StudentOverrides { get; set; }
    }
}
