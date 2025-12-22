using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class LinkStudentDto
    {
        [Required]
        [EmailAddress]
        public string StudentEmail { get; set; } = string.Empty;

        [Required]
        public string Relationship { get; set; } = string.Empty; // e.g., "Parent", "Guardian"

        // Optional: Add a verification code if your flow requires it
        public string? VerificationCode { get; set; }
    }
}
