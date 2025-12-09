using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Response for video upload status
    /// </summary>
    public class VideoUploadStatusResponse
    {
        public string? Status { get; set; }
        public string? Message { get; set; }
        public VideoResponse? Video { get; set; }
    }
}
