using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class ProfileViewConfiguration : IEntityTypeConfiguration<ProfileView>
    {
        public void Configure(EntityTypeBuilder<ProfileView> builder)
        {
            builder.HasIndex(pv => new { pv.StudentId, pv.ViewedAt }); // Analytics Index

            builder.HasOne(pv => pv.Student)
                   .WithMany()
                   .HasForeignKey(pv => pv.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);

            // Viewer might be null (anonymous) or a specific user
            builder.HasOne(pv => pv.ViewerUser)
                   .WithMany()
                   .HasForeignKey(pv => pv.ViewerUserId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}