using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class StudentRating : BaseEntity
    {
       

        public int StudentId { get; set; }

        // Renamed from BandStaffUserId to be accurate (it holds the Int PK, not the Guid String)
        public int BandStaffId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        public string? Comments { get; set; }


        // --- Navigation Properties (Critical for EF Core) ---
        [ForeignKey("StudentId")]
        public virtual Student? Student { get; set; }

        [ForeignKey("BandStaffId")]
        public virtual BandStaff? BandStaff { get; set; }
    }
}
