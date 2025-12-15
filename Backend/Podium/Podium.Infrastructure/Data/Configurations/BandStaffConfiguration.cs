using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class BandStaffConfiguration : IEntityTypeConfiguration<BandStaff>
    {
        public void Configure(EntityTypeBuilder<BandStaff> builder)
        {
            builder.HasKey(bs => bs.Id);

            // Global Filter: Automatically hide inactive staff
            builder.HasQueryFilter(bs => bs.IsActive);

            builder.HasIndex(bs => bs.ApplicationUserId);
            builder.HasIndex(bs => new { bs.BandId, bs.ApplicationUserId }).IsUnique();

            // Default Permissions
            builder.Property(bs => bs.CanViewStudents).HasDefaultValue(true);
            builder.Property(bs => bs.CanRateStudents).HasDefaultValue(true);
            builder.Property(bs => bs.CanContact).HasDefaultValue(true);
            builder.Property(bs => bs.CanSendOffers).HasDefaultValue(false);
            builder.Property(bs => bs.CanManageEvents).HasDefaultValue(false);
            builder.Property(bs => bs.CanManageStaff).HasDefaultValue(false);

            builder.HasOne(bs => bs.ApplicationUser)
                   .WithMany()
                   .HasForeignKey(bs => bs.ApplicationUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(bs => bs.Band)
                   .WithMany(b => b.Staff)
                   .HasForeignKey(bs => bs.BandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}