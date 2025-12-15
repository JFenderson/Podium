using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class ContactRequestConfiguration : IEntityTypeConfiguration<ContactRequest>
    {
        public void Configure(EntityTypeBuilder<ContactRequest> builder)
        {
            builder.HasOne(cr => cr.Band)
                   .WithMany()
                   .HasForeignKey(cr => cr.BandId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(cr => cr.RecruiterStaff)
                   .WithMany()
                   .HasForeignKey(cr => cr.RecruiterStaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}