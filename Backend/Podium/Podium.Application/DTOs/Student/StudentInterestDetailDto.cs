using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Student
{
    public class StudentInterestDetailDto
    {
        public int BandId { get; set; }
        public string BandName { get; set; } = string.Empty;
        public DateTime InterestedAt { get; set; }
    }
}
