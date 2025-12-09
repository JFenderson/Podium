using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Response containing pre-signed URL and upload metadata
    /// </summary>
    public class GetUploadResponse
    {
        public string UploadUrl { get; set; } = string.Empty; // Pre-signed URL for uploading
        public string StoragePath { get; set; } = string.Empty; // Path where the video will be stored
        public string UploadId { get; set; } = string.Empty; // Tracking ID for this upload
        public DateTime ExpiresAt { get; set; }
        
    }
}
