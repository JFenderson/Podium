using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class BandBudget : BaseEntity
    {
        public int BandId { get; set; }

        [Required]
        public int FiscalYear { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalBudget { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal AllocatedAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingAmount { get; set; }

        // Concurrency Token
        [Timestamp]
        public byte[] RowVersion { get; set; }

        // Navigation
        [ForeignKey(nameof(BandId))]
        public virtual Band Band { get; set; } = null!;
    }
}
