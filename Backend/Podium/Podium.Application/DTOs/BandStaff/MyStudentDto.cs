namespace Podium.Application.DTOs.BandStaff
{
    /// <summary>
    /// Student that this staff member is recruiting
    /// </summary>
    public class MyStudentDto
    {
        public int StudentId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? ProfilePhotoUrl { get; set; }
        public string PrimaryInstrument { get; set; } = string.Empty;
        public string? State { get; set; }
        public int GraduationYear { get; set; }
        public decimal? GPA { get; set; }

        // My interaction with this student
        public DateTime? ContactedDate { get; set; }
        public string ContactStatus { get; set; } = string.Empty; // Pending, Approved, Declined
        public DateTime? OfferSentDate { get; set; }
        public decimal? OfferAmount { get; set; }
        public string? OfferStatus { get; set; }
        public int? MyRating { get; set; }
        public DateTime? LastRatedDate { get; set; }

        // Student stats
        public int VideoCount { get; set; }
        public double? AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public DateTime? LastActivityDate { get; set; }

        // Quick actions
        public bool CanContact { get; set; }
        public bool CanMakeOffer { get; set; }
        public bool CanRate { get; set; }
    }

}