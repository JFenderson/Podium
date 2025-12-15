using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.HasIndex(a => a.ApplicationUserId);
            builder.HasIndex(a => a.CreatedAt);

            // Audit logs should generally NOT be deleted even if user is deleted
            // But if you prefer cleanup: .OnDelete(DeleteBehavior.Cascade)
            builder.HasOne(a => a.ApplicationUser)
                   .WithMany()
                   .HasForeignKey(a => a.ApplicationUserId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}