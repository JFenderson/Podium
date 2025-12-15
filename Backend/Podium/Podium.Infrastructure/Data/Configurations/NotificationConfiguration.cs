using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            // Performance Index for "Unread Count" queries
            builder.HasIndex(n => new { n.UserId, n.IsRead });

            builder.HasOne(n => n.User)
                   .WithMany()
                   .HasForeignKey(n => n.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}