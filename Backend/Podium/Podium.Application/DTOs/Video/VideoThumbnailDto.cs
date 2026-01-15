using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    public class VideoThumbnailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string Instrument { get; set; } = string.Empty;
        public int ViewCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
