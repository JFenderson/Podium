using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class GuardianNotificationConfiguration : IEntityTypeConfiguration<GuardianNotification>
    {
        public void Configure(EntityTypeBuilder<GuardianNotification> builder)
        {
            builder.HasOne(gn => gn.Guardian)
                   .WithMany() // .WithMany(g => g.Notifications) if exists
                   .HasForeignKey(gn => gn.GuardianId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}