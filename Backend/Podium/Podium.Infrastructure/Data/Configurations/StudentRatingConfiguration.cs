using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class StudentRatingConfiguration : IEntityTypeConfiguration<StudentRating>
    {
        public void Configure(EntityTypeBuilder<StudentRating> builder)
        {
            builder.HasKey(sr => sr.Id);

            builder.HasOne(sr => sr.Student)
                   .WithMany() // Add .WithMany(s => s.Ratings) to Student if desired
                   .HasForeignKey(sr => sr.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Important: Keep rating history even if staff is deleted
            builder.HasOne(sr => sr.BandStaff)
                   .WithMany()
                   .HasForeignKey(sr => sr.BandStaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}