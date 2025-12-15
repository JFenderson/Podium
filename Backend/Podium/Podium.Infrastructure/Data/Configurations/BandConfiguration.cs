using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class BandConfiguration : IEntityTypeConfiguration<Band>
    {
        public void Configure(EntityTypeBuilder<Band> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.ScholarshipBudget)
                   .HasColumnType("decimal(18,2)");

            builder.Property(b => b.RowVersion)
                   .IsRowVersion();

            builder.HasOne(b => b.Director)
                   .WithMany()
                   .HasForeignKey(b => b.DirectorApplicationUserId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}