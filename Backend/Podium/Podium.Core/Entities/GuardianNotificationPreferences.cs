using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Podium.Core.Entities
{
    [Table("GuardianNotificationPreferences")]
    [Index(nameof(GuardianId), IsUnique = true, Name = "IX_GuardianNotificationPreferences_Guardian")]
    public class GuardianNotificationPreferences : BaseEntity
    {
   

        // --- FIXED: Use GuardianId (int) ---
        [Required]
        public int GuardianId { get; set; }

        // ... [Rest of your properties remain the same] ...
        public bool EmailEnabled { get; set; } = true;
        public bool SmsEnabled { get; set; } = false;
        public bool InAppEnabled { get; set; } = true;
        public bool PushEnabled { get; set; } = false;

        public bool NotifyOnNewOffer { get; set; } = true;
        public bool NotifyOnContactRequest { get; set; } = true;
        public bool NotifyOnOfferExpiring { get; set; } = true;
        public int OfferExpiringDaysThreshold { get; set; } = 7;
        public bool NotifyOnVideoUpload { get; set; } = true;
        public bool NotifyOnInterestShown { get; set; } = false;
        public bool NotifyOnEventRegistration { get; set; } = true;
        public bool NotifyOnActualContact { get; set; } = false;
        public bool NotifyOnProfileUpdate { get; set; } = false;

        [Required]
        [MaxLength(20)]
        public string DigestFrequency { get; set; } = "Immediate";

        public TimeSpan? DailyDigestTime { get; set; } = new TimeSpan(8, 0, 0);
        public int? WeeklyDigestDay { get; set; } = 1;

        public TimeSpan? QuietHoursStart { get; set; }
        public TimeSpan? QuietHoursEnd { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? StudentOverridesJson { get; set; }

        [MaxLength(50)]
        public string? TimeZone { get; set; } = "UTC";

        [MaxLength(10)]
        public string Language { get; set; } = "en";

        public DateTime? LastNotificationSent { get; set; }
        public bool IsUnsubscribed { get; set; } = false;
        public DateTime? UnsubscribedDate { get; set; }

        // --- FIXED: Add Navigation Property ---
        [ForeignKey(nameof(GuardianId))]
        public virtual Guardian? Guardian { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}