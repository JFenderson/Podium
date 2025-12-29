using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Request to rate a video (bandstaff only)
    /// </summary>
    public class RateVideoRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [MaxLength(500)]
        public string? Comment { get; set; }
    }
}
