using Podium.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Core.Entities
{
    public class Student : BaseEntity, ISoftDelete
    {

        [Required]
        public string ApplicationUserId { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Instrument { get; set; }

        [StringLength(1000)]
        public string? Bio { get; set; }

        [Column(TypeName = "decimal(3,2)")]
        public decimal? GPA { get; set; }
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        public bool RequiresGuardianApproval { get; set; } = true;  // For minors
        public DateTime? LastActivityDate { get; set; }
        public string? SecondaryInstruments { get; set; }  // JSON string
        public string? Achievements { get; set; }  // JSON string
  
        public string? IntendedMajor { get; set; }
        public string? PrimaryInstrument { get; set; }
        public string? SkillLevel { get; set; }
        public int YearsExperience { get; set; }
        public int GraduationYear { get; set; }
        public string? HighSchool { get; set; }
        public string? State { get; set; }
        public string? SchoolType { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation property
        [ForeignKey(nameof(ApplicationUserId))]
        public virtual ApplicationUser? ApplicationUser { get; set; }

        // Navigation property for guardian relationship
        public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
        public virtual ICollection<StudentInterest> StudentInterests { get; set; } = new List<StudentInterest>();
        public virtual ICollection<ContactRequest> ContactRequests { get; set; } = new List<ContactRequest>();
        public virtual ICollection<EventRegistration> EventRegistrations { get; set; } = new List<EventRegistration>();
        public virtual ICollection<ScholarshipOffer> ScholarshipOffers { get; set; } = new List<ScholarshipOffer>();
        public virtual ICollection<ContactLog> ContactLogs { get; set; } = new List<ContactLog>();
        public virtual ICollection<Guardian> Guardians { get; set; } = new List<Guardian>();

        public virtual ICollection<StudentGuardian>? StudentGuardianLinks { get; set; }
    }
}
