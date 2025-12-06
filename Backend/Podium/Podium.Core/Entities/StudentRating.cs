using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class StudentRating
    {
        public int StudentRatingId { get; set; }
        public int StudentId { get; set; }
        public int BandStaffUserId { get; set; }
        public int Rating { get; set; }
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
