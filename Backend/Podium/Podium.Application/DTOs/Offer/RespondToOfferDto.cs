using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Offer
{
    public class RespondToOfferDto
    {
        public bool Accept { get; set; }
        public string? Comment { get; set; }
    }
}
