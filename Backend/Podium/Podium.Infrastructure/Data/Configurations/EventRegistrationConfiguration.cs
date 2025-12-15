using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Podium.Core.Entities;

namespace Podium.Infrastructure.Data.Configurations
{
    public class EventRegistrationConfiguration : IEntityTypeConfiguration<EventRegistration>
    {
        public void Configure(EntityTypeBuilder<EventRegistration> builder)
        {
            builder.HasKey(er => er.Id);

            // Prevent duplicate registration
            builder.HasIndex(er => new { er.BandEventId, er.StudentId }).IsUnique();

            builder.HasOne(er => er.BandEvent)
                   .WithMany(be => be.Registrations)
                   .HasForeignKey(er => er.BandEventId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(er => er.Student)
                   .WithMany(s => s.EventRegistrations)
                   .HasForeignKey(er => er.StudentId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}