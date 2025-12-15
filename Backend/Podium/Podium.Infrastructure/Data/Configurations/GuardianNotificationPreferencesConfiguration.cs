using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class GuardianNotificationPreferencesConfiguration : IEntityTypeConfiguration<GuardianNotificationPreferences>
    {
        public void Configure(EntityTypeBuilder<GuardianNotificationPreferences> builder)
        {
            builder.HasOne(p => p.Guardian)
                   .WithMany()
                   .HasForeignKey(p => p.GuardianId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}