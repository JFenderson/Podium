using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    /// <summary>
    /// Basic information about a linked student.
    /// </summary>
    public class LinkedStudentDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string PrimaryInstrument { get; set; } = string.Empty;
        public int GraduationYear { get; set; }
        public string HighSchool { get; set; } = string.Empty;

        // Link details
        public string RelationshipType { get; set; } = string.Empty; // Parent, Guardian, Other
        public bool IsVerified { get; set; }
        public DateTime LinkedDate { get; set; }

        // Permissions
        public bool CanViewActivity { get; set; }
        public bool CanApproveContacts { get; set; }
        public bool CanRespondToOffers { get; set; }
        public bool ReceivesNotifications { get; set; }
    }
}
