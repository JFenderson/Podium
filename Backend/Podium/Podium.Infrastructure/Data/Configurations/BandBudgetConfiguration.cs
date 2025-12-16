using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class BandBudgetConfiguration : IEntityTypeConfiguration<BandBudget>
    {
        public void Configure(EntityTypeBuilder<BandBudget> builder)
        {
            builder.HasKey(b => b.Id);

            builder.Property(b => b.TotalBudget).HasColumnType("decimal(18,2)");
            builder.Property(b => b.AllocatedAmount).HasColumnType("decimal(18,2)");
            builder.Property(b => b.RemainingAmount).HasColumnType("decimal(18,2)");

            // Optimistic Concurrency
            builder.Property(b => b.RowVersion).IsRowVersion();

            builder.HasOne(b => b.Band)
                   .WithMany(b => b.Budgets)
                   .HasForeignKey(b => b.BandId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}