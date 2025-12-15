using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class StudentGuardianConfiguration : IEntityTypeConfiguration<StudentGuardian>
    {
        public void Configure(EntityTypeBuilder<StudentGuardian> builder)
        {
            // Composite Key
            builder.HasKey(sg => new { sg.StudentId, sg.GuardianId });

            builder.HasOne(sg => sg.Student)
                   .WithMany()
                   .HasForeignKey(sg => sg.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(sg => sg.Guardian)
                   .WithMany(g => g.StudentLinks)
                   .HasForeignKey(sg => sg.GuardianId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}