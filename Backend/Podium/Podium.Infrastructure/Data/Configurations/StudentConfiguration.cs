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
    public class StudentConfiguration : IEntityTypeConfiguration<Student>
    {
        public void Configure(EntityTypeBuilder<Student> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.GPA).HasColumnType("decimal(3,2)"); // 0.00 - 4.00

            // Indexes for frequent searches
            builder.HasIndex(s => s.ApplicationUserId);
            builder.HasIndex(s => s.Instrument);
            builder.HasIndex(s => s.GraduationYear);
            builder.HasIndex(s => new { s.State, s.Instrument }); // Common Recruiter Filter

            builder.HasOne(s => s.ApplicationUser)
                   .WithMany()
                   .HasForeignKey(s => s.ApplicationUserId)
                   .OnDelete(DeleteBehavior.Restrict);

            // Configure Many-to-Many with Guardian
            builder.HasMany(s => s.Guardians)
                   .WithMany(g => g.Students)
                   .UsingEntity<StudentGuardian>();


            builder.HasMany(s => s.Guardians)
                   .WithMany(g => g.Students)
                   .UsingEntity<StudentGuardian>(
                       // Configure Student → StudentGuardian
                       j => j.HasOne(sg => sg.Guardian)
                             .WithMany()
                             .HasForeignKey(sg => sg.GuardianId)
                             .OnDelete(DeleteBehavior.Cascade),
                       // Configure Guardian → StudentGuardian
                       j => j.HasOne(sg => sg.Student)
                             .WithMany()
                             .HasForeignKey(sg => sg.StudentId)
                             .OnDelete(DeleteBehavior.Cascade),
                       // Configure StudentGuardian itself
                       j =>
                       {
                           j.HasKey(sg => sg.Id);
                           j.ToTable("StudentGuardians");
                           j.HasIndex(sg => new { sg.StudentId, sg.GuardianId })
                            .IsUnique()
                            .HasDatabaseName("IX_StudentGuardian_Student_Guardian");
                       });

        }
    }
}
