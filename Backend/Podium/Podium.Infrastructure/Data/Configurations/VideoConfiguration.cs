using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class VideoConfiguration : IEntityTypeConfiguration<Video>
    {
        public void Configure(EntityTypeBuilder<Video> builder)
        {
            builder.Property(v => v.AverageRating)
                   .HasColumnType("decimal(3,2)")
                   .HasDefaultValue(0);

            builder.HasOne(v => v.Student)
                   .WithMany(s => s.Videos)
                   .HasForeignKey(v => v.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}