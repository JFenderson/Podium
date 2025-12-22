using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Application.DTOs.Director
{
    public class DailyEngagementDto
    {
        public DateTime Date { get; set; }
        public int Views { get; set; }
        public int Interests { get; set; }
    }
}
