using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Response for a video rating
    /// </summary>
    public class VideoRatingResponse
    {
        public int RatingId { get; set; }
        public int VideoId { get; set; }
        public int BandStaffId { get; set; }
        public string? BandStaffName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool Success { get; internal set; }
        public string? Message { get; internal set; }
    }
}
