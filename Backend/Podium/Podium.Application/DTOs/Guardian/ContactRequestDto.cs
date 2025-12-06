using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Contact request requiring guardian approval.
    /// </summary>
    public class ContactRequestDto
    {
        public int RequestId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public string University { get; set; } = string.Empty;
        public string RecruiterName { get; set; } = string.Empty;
        public string RecruiterTitle { get; set; } = string.Empty;
        public string Purpose { get; set; } = string.Empty;
        public string PreferredContactMethod { get; set; } = string.Empty;
        public DateTime RequestedDate { get; set; }
        public string Status { get; set; } = string.Empty; // Pending, Approved, Declined
        public DateTime? ResponseDate { get; set; }
        public string? ResponseNotes { get; set; }
        public bool IsUrgent { get; set; }
    }

    public class ContactRequestResponseDto
    {
        public string? Notes { get; set; }
        public string? Reason { get; set; } // For declines
    }
}
