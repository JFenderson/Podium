using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class ScholarshipOfferConfiguration : IEntityTypeConfiguration<ScholarshipOffer>
    {
        public void Configure(EntityTypeBuilder<ScholarshipOffer> builder)
        {
            builder.Property(o => o.ScholarshipAmount).HasColumnType("decimal(18,2)");

            builder.HasOne(o => o.Band)
                   .WithMany(b => b.Offers)
                   .HasForeignKey(o => o.BandId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(o => o.Student)
                   .WithMany(s => s.ScholarshipOffers)
                   .HasForeignKey(o => o.StudentId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Keep offer history even if staff deleted
            builder.HasOne(o => o.CreatedByStaff)
                   .WithMany(bs => bs.OffersCreated)
                   .HasForeignKey(o => o.CreatedByStaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}