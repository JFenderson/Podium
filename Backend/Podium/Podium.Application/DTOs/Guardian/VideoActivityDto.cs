using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class VideoActivityDto
    {
        public int VideoId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Instrument { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public int Views { get; set; }
        public bool? IsPublic { get; set; }
    }
}
