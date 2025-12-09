using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// List item for student's own videos
    /// </summary>
    public class MyVideoListItem
    {
        public int VideoId { get; set; }
        public string? Title { get; set; }
        public string? Category { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? TranscodingStatus { get; set; }

        public int ViewCount { get; set; }
        public decimal? AverageRating { get; set; }
        public int RatingCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UploadedDate { get; set; }
        public bool IsReviewed { get; set; }
        public bool IsPublic { get; set; }
    }
}
