using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class ContactActivityDto
    {
        public int ContactId { get; set; }
        public string RecruiterName { get; set; } = string.Empty;
        public string BandName { get; set; } = string.Empty;
        public DateTime ContactDate { get; set; }
        public string ContactMethod { get; set; } = string.Empty; // Email, Phone, InPerson
        public string? Purpose { get; set; }
    }
}
