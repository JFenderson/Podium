using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Podium.Application.Validation;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Request to create video entity after successful upload to storage
    /// </summary>
    public class CreateVideoRequest
    {
        [Required(ErrorMessage = "Upload ID is required")]
        public string? UploadId { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [SafeString]
        public string Title { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Instrument is required")]
        [StringLength(100, ErrorMessage = "Instrument cannot exceed 100 characters")]
        [Instrument]
        public string Instrument { get; set; } = string.Empty;

        public bool IsPublic { get; set; } = true;

        // Used to generate the storage path
        [Required(ErrorMessage = "File name is required")]
        [StringLength(255, ErrorMessage = "File name cannot exceed 255 characters")]
        [SafeString]
        public string FileName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Content type is required")]
        [StringLength(100, ErrorMessage = "Content type cannot exceed 100 characters")]
        public string ContentType { get; set; } = string.Empty;

        [Range(0, long.MaxValue, ErrorMessage = "File size must be a positive number")]
        public long FileSizeBytes { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        [SafeString]
        public string? Category { get; set; }
    }
}
