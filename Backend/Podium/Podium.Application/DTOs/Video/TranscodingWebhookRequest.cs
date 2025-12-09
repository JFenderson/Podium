using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Video
{
    /// <summary>
    /// Webhook payload from transcoding service
    /// </summary>
    public class TranscodingWebhookRequest
    {
        [Required]
        public string? JobId { get; set; }
        public string? Status { get; set; } // "completed" or "failed"

        public int? DurationSeconds { get; set; }
        public string ThumbnailPath { get; set; } = string.Empty;
        public string? OutputUrl { get; set; }
        public string? ErrorMessage { get; set; }


        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();  
    }
}
