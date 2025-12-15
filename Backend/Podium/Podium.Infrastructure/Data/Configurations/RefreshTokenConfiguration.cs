using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Podium.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Token).IsRequired().HasMaxLength(500);
            builder.Property(e => e.ApplicationUserId).IsRequired();

            builder.HasOne(e => e.ApplicationUser)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.ApplicationUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(e => e.Token).IsUnique();
            builder.HasIndex(e => e.ApplicationUserId);

    
        }
    }
}
