using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class SavedSearchConfiguration : IEntityTypeConfiguration<SavedSearch>
    {
        public void Configure(EntityTypeBuilder<SavedSearch> builder)
        {
            builder.HasKey(e => e.Id);

            // Navigation to BandStaff (your "Recruiter" role)
            builder.HasOne(e => e.BandStaff)
                .WithMany()
                .HasForeignKey(e => e.BandStaffId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.Description)
                .HasMaxLength(500);

            builder.Property(e => e.FilterCriteria)
                .IsRequired()
                .HasColumnType("nvarchar(max)");

            builder.Property(e => e.ShareToken)
                .HasMaxLength(50);

            builder.HasIndex(e => e.ShareToken)
                .IsUnique()
                .HasFilter("[ShareToken] IS NOT NULL");

            builder.HasIndex(e => new { e.BandStaffId, e.IsTemplate })
                .HasDatabaseName("IX_SavedSearches_RecruiterId_IsTemplate");

            builder.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_SavedSearches_CreatedAt");

            // Default values
            builder.Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(e => e.TimesUsed)
                .HasDefaultValue(0);

            builder.Property(e => e.LastResultCount)
                .HasDefaultValue(0);

            builder.Property(e => e.IsShared)
                .HasDefaultValue(false);

            builder.Property(e => e.IsTemplate)
                .HasDefaultValue(false);

            builder.Property(e => e.AlertsEnabled)
                .HasDefaultValue(false);
        }
    }

   
}