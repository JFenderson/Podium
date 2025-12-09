using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Offer
{
    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;

        public ScholarshipStatus ToScholarshipStatus()
        {
            return Status.ToLower() switch
            {
                "pending" => ScholarshipStatus.PendingApproval,
                "accepted" => ScholarshipStatus.Accepted,
                "declined" => ScholarshipStatus.Declined,
                "revoked" => ScholarshipStatus.Rescinded,
                _ => throw new ArgumentException("Invalid status value")
            };
        }
    }
}
