using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class ContactLogConfiguration : IEntityTypeConfiguration<ContactLog>
    {
        public void Configure(EntityTypeBuilder<ContactLog> builder)
        {
            // Prevent cascade delete from Band -> ContactLog
            builder.HasOne(cl => cl.Band)
                   .WithMany()
                   .HasForeignKey(cl => cl.BandId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete from BandStaff -> ContactLog
            builder.HasOne(cl => cl.RecruiterStaff)
                   .WithMany(bs => bs.ContactsInitiated)
                   .HasForeignKey(cl => cl.RecruiterStaffId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}