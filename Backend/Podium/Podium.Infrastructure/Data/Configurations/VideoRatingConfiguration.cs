using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class VideoRatingConfiguration : IEntityTypeConfiguration<VideoRating>
    {
        public void Configure(EntityTypeBuilder<VideoRating> builder)
        {
            builder.HasKey(r => r.Id); // Ensure this matches entity PK

            builder.HasOne(r => r.Video)
                   .WithMany(v => v.Ratings)
                   .HasForeignKey(r => r.VideoId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.BandStaff)
                   .WithMany()
                   .HasForeignKey(r => r.BandStaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}