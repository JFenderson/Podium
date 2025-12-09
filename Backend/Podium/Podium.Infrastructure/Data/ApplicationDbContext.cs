using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Podium.Core.Entities;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace Podium.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Guardian> Guardians { get; set; } = null!;
    public DbSet<StudentGuardian> StudentGuardians { get; set; } = null!;
    public DbSet<Band> Bands { get; set; } = null!;
    public DbSet<BandStaff> BandStaff { get; set; } = null!;
    public DbSet<Video> Videos { get; set; } = null!;
    public DbSet<VideoRating> VideoRatings { get; set; } = null!;
    public DbSet<StudentInterest> StudentInterests { get; set; } = null!;
    public DbSet<ScholarshipOffer> Offers { get; set; } = null!;
    public DbSet<ContactRequest> ContactRequests { get; set; } = null!;
    public DbSet<ContactLog> ContactLogs { get; set; } = null!;
    public DbSet<BandEvent> BandEvents { get; set; } = null!;
    public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;
    public DbSet<GuardianNotification> GuardianNotifications { get; set; } = null!;
    public DbSet<GuardianNotificationPreferences> GuardianNotificationPreferences { get; set; } = null!;
    public DbSet<ProfileView> ProfileViews { get; set; } = null!;
    public DbSet<StudentRating> StudentRatings { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ===== ApplicationUser Configuration =====
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });


        // Configure RefreshToken entity
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationUserId).IsRequired();

            entity.HasOne(e => e.ApplicationUser)
                  .WithMany()
                  .HasForeignKey(e => e.ApplicationUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ApplicationUserId);
        });

        
        // ===== Decimal Precision =====
        builder.Entity<Band>()
            .Property(b => b.ScholarshipBudget)
            .HasColumnType("decimal(18,2)");

        builder.Entity<ScholarshipOffer>()
            .Property(o => o.ScholarshipAmount)
            .HasColumnType("decimal(18,2)");

        // ===== Student Configuration =====
        builder.Entity<Student>()
            .HasOne(s => s.ApplicationUser)
            .WithMany()
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Student>()
            .Property(s => s.GPA)
            .HasColumnType("decimal(3,2)");

        // Guardian - ApplicationUser relationship
        builder.Entity<Guardian>()
            .HasOne(g => g.ApplicationUser)
            .WithMany()
            .HasForeignKey(g => g.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Guardian-Student Many-to-Many with Junction =====
        builder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(sg => sg.Id);

            entity.HasOne(sg => sg.Student)
                  .WithMany()
                  .HasForeignKey(sg => sg.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sg => sg.Guardian)
                  .WithMany()
                  .HasForeignKey(sg => sg.GuardianId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(sg => new { sg.StudentId, sg.GuardianId }).IsUnique();
        });

        // ===== Guardian-Student Many-to-Many Relationship =====
        builder.Entity<Student>()
            .HasMany(s => s.Guardians)
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

        // BandStaff - ApplicationUser relationship
        builder.Entity<BandStaff>()
            .HasOne(bs => bs.ApplicationUser)
            .WithMany()
            .HasForeignKey(bs => bs.ApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<BandStaff>()
            .HasOne(bs => bs.Band)
            .WithMany(b => b.Staff)
            .HasForeignKey(bs => bs.BandId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Band Staff Values =====
        // BandStaff default permission values
        builder.Entity<BandStaff>()
            .Property(bs => bs.CanViewStudents)
            .HasDefaultValue(true);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanContact)
            .HasDefaultValue(true);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanRateStudents)
            .HasDefaultValue(true);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanSendOffers)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanManageEvents)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanManageStaff)
            .HasDefaultValue(false);

        // ===== Band Configuration =====
        builder.Entity<Band>()
            .HasOne(b => b.Director)
            .WithMany()
            .HasForeignKey(b => b.DirectorApplicationUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // StudentGuardian configuration
        builder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(sg => new { sg.StudentId, sg.GuardianId });

            entity.HasOne(sg => sg.Student)
                  .WithMany()
                  .HasForeignKey(sg => sg.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== Offer Configuration =====
        builder.Entity<ScholarshipOffer>()
            .HasOne(o => o.Band)
            .WithMany(b => b.Offers)
            .HasForeignKey(o => o.BandId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ScholarshipOffer>()
            .HasOne(o => o.Student)
            .WithMany(s => s.ScholarshipOffers)
            .HasForeignKey(o => o.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ScholarshipOffer>()
            .HasOne(o => o.CreatedByStaff)
            .WithMany(bs => bs.OffersCreated)
            .HasForeignKey(o => o.CreatedByStaffId)
            .OnDelete(DeleteBehavior.Restrict);

        // ===== Video Configuration =====
        builder.Entity<Video>()
            .HasOne(v => v.Student)
            .WithMany(s => s.Videos)
            .HasForeignKey(v => v.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== AuditLog Configuration =====
        builder.Entity<AuditLog>()
            .HasIndex(a => a.ApplicationUserId);

        builder.Entity<AuditLog>()
            .HasIndex(a => a.Timestamp);

        // ===== Indexes for Performance =====
        builder.Entity<Student>()
            .HasIndex(s => s.ApplicationUserId);

        builder.Entity<Guardian>()
            .HasIndex(g => g.ApplicationUserId);

        builder.Entity<BandStaff>()
            .HasIndex(bs => bs.ApplicationUserId);

        builder.Entity<BandStaff>()
            .HasIndex(bs => new { bs.BandId, bs.ApplicationUserId })
            .IsUnique();

        // ===== Video Configuration =====
        builder.Entity<Video>()
            .HasOne(v => v.Student)
            .WithMany(s => s.Videos)
            .HasForeignKey(v => v.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== VideoRating Configuration (NEW) =====
        builder.Entity<VideoRating>(entity =>
        {
            entity.HasKey(r => r.Id);

            // Relationship: Video -> Ratings
            // If a video is deleted, delete all its ratings (Cascade)
            entity.HasOne(r => r.Video)
                  .WithMany() // You can add a collection to Video entity if you want: public virtual ICollection<VideoRating> Ratings { get; set; }
                  .HasForeignKey(r => r.VideoId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relationship: BandStaff (Recruiter) -> Ratings
            // If a Staff member is deleted, do NOT auto-delete their historical ratings (Restrict)
            // This is safer for data integrity and audit trails.
            entity.HasOne(r => r.BandStaff)
                  .WithMany()
                  .HasForeignKey(r => r.BandStaffId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ===== ContactLog Configuration (Fix for Cycles) =====
        builder.Entity<ContactLog>(entity =>
        {
            // Prevent cascade delete from Band -> ContactLog
            // (Because Band -> BandStaff -> ContactLog already exists)
            entity.HasOne(cl => cl.Band)
                  .WithMany()
                  .HasForeignKey(cl => cl.BandId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Prevent cascade delete from BandStaff -> ContactLog
            // (To preserve history even if a recruiter account is deleted)
            entity.HasOne(cl => cl.RecruiterStaff)
                  .WithMany(bs => bs.ContactsInitiated)
                  .HasForeignKey(cl => cl.RecruiterStaffId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<ContactRequest>(entity =>
        {
            entity.HasOne(cr => cr.Band)
                  .WithMany()
                  .HasForeignKey(cr => cr.BandId)
                  .OnDelete(DeleteBehavior.Restrict); // Break Band -> ContactRequest path

            entity.HasOne(cr => cr.RecruiterStaff)
                  .WithMany() // Add ICollection to BandStaff if needed, or leave empty
                  .HasForeignKey(cr => cr.RecruiterStaffId)
                  .OnDelete(DeleteBehavior.Restrict); // Break Staff -> ContactRequest path
        });

        builder.Entity<ScholarshipOffer>(entity =>
        {
            entity.HasOne(o => o.Band)
                  .WithMany(b => b.Offers)
                  .HasForeignKey(o => o.BandId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.CreatedByStaff)
                  .WithMany(bs => bs.OffersCreated)
                  .HasForeignKey(o => o.CreatedByStaffId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Optional: Keep Student cascade or restrict it depending on preference
            // entity.HasOne(o => o.Student).WithMany(s => s.ScholarshipOffers).OnDelete(DeleteBehavior.Restrict);
        });
    }
}