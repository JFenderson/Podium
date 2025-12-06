using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Podium.Core.Entities
{
    /// <summary>
    /// Stores notification preferences for guardians.
    /// Allows granular control over which events trigger notifications and through which channels.
    /// One record per guardian user (not per student link).
    /// </summary>
    [Table("GuardianNotificationPreferences")]
    [Index(nameof(GuardianUserId), IsUnique = true, Name = "IX_GuardianNotificationPreferences_Guardian")]
    public class GuardianNotificationPreferences
    {
        [Key]
        public int GuardianNotificationPreferencesId { get; set; }

        /// <summary>
        /// The guardian user ID (from Identity system).
        /// </summary>
        [Required]
        [MaxLength(450)]
        public string GuardianUserId { get; set; } = string.Empty;

        // ============== NOTIFICATION CHANNELS ==============

        /// <summary>
        /// Whether to send email notifications.
        /// </summary>
        public bool EmailEnabled { get; set; } = true;

        /// <summary>
        /// Whether to send SMS notifications.
        /// Requires verified phone number.
        /// </summary>
        public bool SmsEnabled { get; set; } = false;

        /// <summary>
        /// Whether to show in-app notifications.
        /// </summary>
        public bool InAppEnabled { get; set; } = true;

        /// <summary>
        /// Whether to send push notifications (if mobile app is available).
        /// </summary>
        public bool PushEnabled { get; set; } = false;

        // ============== EVENT PREFERENCES ==============
        // Control which types of events trigger notifications

        /// <summary>
        /// Notify when a student receives a new scholarship offer.
        /// </summary>
        public bool NotifyOnNewOffer { get; set; } = true;

        /// <summary>
        /// Notify when a recruiter requests permission to contact a student.
        /// </summary>
        public bool NotifyOnContactRequest { get; set; } = true;

        /// <summary>
        /// Notify when a scholarship offer is approaching expiration.
        /// </summary>
        public bool NotifyOnOfferExpiring { get; set; } = true;

        /// <summary>
        /// How many days before expiration to send notification.
        /// </summary>
        public int OfferExpiringDaysThreshold { get; set; } = 7;

        /// <summary>
        /// Notify when student uploads a new video.
        /// </summary>
        public bool NotifyOnVideoUpload { get; set; } = true;

        /// <summary>
        /// Notify when student shows interest in a new band.
        /// </summary>
        public bool NotifyOnInterestShown { get; set; } = false;

        /// <summary>
        /// Notify when student registers for an event.
        /// </summary>
        public bool NotifyOnEventRegistration { get; set; } = true;

        /// <summary>
        /// Notify when student is contacted by a recruiter (after approval).
        /// </summary>
        public bool NotifyOnActualContact { get; set; } = false;

        /// <summary>
        /// Notify on general student profile changes.
        /// </summary>
        public bool NotifyOnProfileUpdate { get; set; } = false;

        // ============== FREQUENCY SETTINGS ==============

        /// <summary>
        /// Notification delivery frequency.
        /// Values: "Immediate", "Daily", "Weekly", "None"
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string DigestFrequency { get; set; } = "Immediate";

        /// <summary>
        /// Time of day to send daily digest (if DigestFrequency is Daily).
        /// Stored as TimeSpan (e.g., 08:00:00 for 8 AM).
        /// </summary>
        public TimeSpan? DailyDigestTime { get; set; } = new TimeSpan(8, 0, 0); // 8 AM

        /// <summary>
        /// Day of week to send weekly digest (if DigestFrequency is Weekly).
        /// 0 = Sunday, 6 = Saturday
        /// </summary>
        public int? WeeklyDigestDay { get; set; } = 1; // Monday

        /// <summary>
        /// Start of quiet hours (no notifications during this period).
        /// </summary>
        public TimeSpan? QuietHoursStart { get; set; }

        /// <summary>
        /// End of quiet hours.
        /// </summary>
        public TimeSpan? QuietHoursEnd { get; set; }

        // ============== PER-STUDENT OVERRIDES ==============
        // Stored as JSON for flexibility; allows different settings per student

        /// <summary>
        /// JSON string containing per-student notification overrides.
        /// Format: { "studentId": { "NotifyOnNewOffer": false, ... }, ... }
        /// Allows guardians to have different notification settings for different students.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? StudentOverridesJson { get; set; }

        // ============== METADATA ==============

        /// <summary>
        /// Timezone for the guardian (for proper digest timing).
        /// </summary>
        [MaxLength(50)]
        public string? TimeZone { get; set; } = "UTC";

        /// <summary>
        /// Language preference for notifications.
        /// </summary>
        [MaxLength(10)]
        public string Language { get; set; } = "en";

        // ============== AUDIT FIELDS ==============

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last time a notification was successfully sent.
        /// Used for throttling and debugging.
        /// </summary>
        public DateTime? LastNotificationSent { get; set; }

        /// <summary>
        /// Whether the guardian has unsubscribed from all notifications.
        /// </summary>
        public bool IsUnsubscribed { get; set; } = false;

        /// <summary>
        /// Date when guardian unsubscribed (if applicable).
        /// </summary>
        public DateTime? UnsubscribedDate { get; set; }
    }
}
