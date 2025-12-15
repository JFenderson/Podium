using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class GuardianConfiguration : IEntityTypeConfiguration<Guardian>
    {
        public void Configure(EntityTypeBuilder<Guardian> builder)
        {
            builder.HasKey(g => g.Id);
            builder.HasIndex(g => g.ApplicationUserId);

            builder.HasOne(g => g.ApplicationUser)
                   .WithMany()
                   .HasForeignKey(g => g.ApplicationUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}