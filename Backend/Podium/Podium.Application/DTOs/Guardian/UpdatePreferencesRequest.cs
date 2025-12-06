using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Request to update guardian notification preferences.
    /// All fields are nullable to support partial updates.
    /// </summary>
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