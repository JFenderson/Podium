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
    /// Request to update video metadata
    /// </summary>
    public class UpdateVideoRequest
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
        [SafeString]
        public string? Title { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category cannot exceed 50 characters")]
        [SafeString]
        public string? Category { get; set; }

        public bool? IsPublic { get; set; }
    }
}
