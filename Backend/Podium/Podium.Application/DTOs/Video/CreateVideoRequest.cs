using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Request to create video entity after successful upload to storage
    /// </summary>
    public class CreateVideoRequest
    {
        [Required]
        public string? UploadId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(100)]
        public string Instrument { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        // Used to generate the storage path
        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string ContentType { get; set; } = string.Empty;

        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(50)]
        public string? Category { get; set; }
    }
}
