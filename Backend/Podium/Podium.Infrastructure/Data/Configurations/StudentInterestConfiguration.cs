using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class StudentInterestConfiguration : IEntityTypeConfiguration<StudentInterest>
    {
        public void Configure(EntityTypeBuilder<StudentInterest> builder)
        {
            builder.HasKey(si => si.Id);
            builder.HasIndex(si => new { si.StudentId, si.BandId }).IsUnique(); // Prevent duplicates

            builder.HasOne(si => si.Student)
                   .WithMany(s => s.StudentInterests)
                   .HasForeignKey(si => si.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(si => si.Band)
                   .WithMany()
                   .HasForeignKey(si => si.BandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}