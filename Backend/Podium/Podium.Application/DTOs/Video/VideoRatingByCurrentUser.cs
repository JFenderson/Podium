using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Current user's rating for a video (if exists)
    /// </summary>
    public class VideoRatingByCurrentUser
    {
        public bool HasRated { get; set; }
        public int? Rating { get; set; }
        public string? Comment { get; set; }
        public int? RatingId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
