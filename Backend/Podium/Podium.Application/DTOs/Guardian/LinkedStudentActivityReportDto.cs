using System;
using System.Collections.Generic;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Detailed activity history for a student over a specified time period.
    /// Includes all tracked activity types: videos, interests, offers, events, and contacts.
    /// </summary>
    public class LinkedStudentActivityReportDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Activity collections
        public List<VideoActivityDto> VideosUploaded { get; set; } = new();
        public List<InterestActivityDto> InterestShown { get; set; } = new();
        public List<OfferActivityDto> OffersReceived { get; set; } = new();
        public List<EventActivityDto> EventsAttended { get; set; } = new();
        public List<ContactActivityDto> ContactsMade { get; set; } = new();

        // Summary counts
        public int TotalVideos { get; set; }
        public int TotalInterests { get; set; }
        public int TotalOffers { get; set; }
        public int TotalEvents { get; set; }
        public int TotalContacts { get; set; }
    }
}
