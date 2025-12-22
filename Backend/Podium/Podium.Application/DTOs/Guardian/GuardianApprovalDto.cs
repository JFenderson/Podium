using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Guardian
{
    public class GuardianApprovalDto
    {
        public int OfferId { get; set; }
        public bool Approved { get; set; }
        public string? Notes { get; set; }
    }
}
