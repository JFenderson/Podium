using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class AuditLog : BaseEntity
    {

        [MaxLength(450)]
        public string? ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ActionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;


        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public bool IsSecurityEvent { get; set; } = false;

        [MaxLength(20)]
        public string? Severity { get; set; } // Low, Medium, High, Critical

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }
    }
}
