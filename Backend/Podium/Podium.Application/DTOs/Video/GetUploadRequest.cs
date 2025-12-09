using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Request to get a pre-signed URL for direct video upload to cloud storage
    /// </summary>
    public class GetUploadRequest
    {
        [Required]
        [MaxLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ContentType { get; set; } = string.Empty;

        [Required]
        public long FileSizeBytes { get; set; }
    }
}
