using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class BandEventConfiguration : IEntityTypeConfiguration<BandEvent>
    {
        public void Configure(EntityTypeBuilder<BandEvent> builder)
        {
            builder.HasOne(e => e.Band)
                   .WithMany(b => b.Events) // Ensure collection exists on Band
                   .HasForeignKey(e => e.BandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}