using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Response DTO for video details
    /// </summary>
    public class VideoResponse
    {
        public int VideoId { get; set; }
        public int StudentId { get; set; }
        public string? StudentName { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string Instrument { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? VideoUrl { get; set; } // Pre-signed URL for viewing
        public long FileSizeBytes { get; set; }
        public int? DurationSeconds { get; set; }
        public string? TranscodingStatus { get; set; }
        public int ViewCount { get; set; }
        public int RatingCount { get; set; }
        public DateTime UploadedDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }

        // Only populated for the video owner or bandstaff
        public VideoRatingByCurrentUser? MyRating { get; set; }
    }
}
