using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class ScholarshipResponseRequest
    {
        [Required]
        public string Response { get; set; } = string.Empty; // "Accepted" or "Declined"

        public string? Notes { get; set; }
    }
}
