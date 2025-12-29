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
    public class SearchAlertConfiguration : IEntityTypeConfiguration<SearchAlert>
    {
        public void Configure(EntityTypeBuilder<SearchAlert> builder)
        {
            builder.HasKey(e => e.Id);

            builder.HasOne(e => e.SavedSearch)
                .WithMany()
                .HasForeignKey(e => e.SavedSearchId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(e => e.EmailError)
                .HasMaxLength(1000);

            builder.Property(e => e.NewMatchIds)
                .HasMaxLength(2000);

            builder.HasIndex(e => new { e.SavedSearchId, e.SentAt })
                .HasDatabaseName("IX_SearchAlerts_SavedSearchId_SentAt");

            // Default values
            builder.Property(e => e.SentAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.WasEmailSent)
                .HasDefaultValue(false);

            builder.Property(e => e.NewMatchesCount)
                .HasDefaultValue(0);
        }
    }
}
