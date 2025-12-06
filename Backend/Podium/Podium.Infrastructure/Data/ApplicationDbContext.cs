using Podium.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace Podium.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Band> Bands { get; set; } = null!;
    public DbSet<Video> Videos { get; set; } = null!;
    public DbSet<StudentInterest> StudentInterests { get; set; } = null!;
    public DbSet<ContactRequest> ContactRequests { get; set; } = null!;
    public DbSet<BandEvent> BandEvents { get; set; } = null!;
    public DbSet<EventRegistration> EventRegistrations { get; set; } = null!;
    public DbSet<ContactLog> ContactLogs { get; set; } = null!;
    public DbSet<ProfileView> ProfileViews { get; set; } = null!;
    public DbSet<GuardianNotification> GuardianNotifications { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Guardian> Guardians { get; set; } = null!;
    public DbSet<BandStaff> BandStaff { get; set; } = null!;
    public DbSet<Offer> Offers { get; set; } = null!;
    public DbSet<Offer> ScholarshipOffers { get; set; } = null!;
    public DbSet<StudentGuardian> StudentGuardians { get; set; } = null!;
    public DbSet<GuardianNotificationPreferences> GuardianNotificationPreferences { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<StudentRating> StudentRatings { get; set; } = null!;
    public DbSet<Document> Documents { get; set; } = null!;
    public DbSet<DocumentTag> DocumentTags { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Document entity
        builder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.FileExtension).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UploadedBy).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.Documents)
                  .HasForeignKey(e => e.UploadedBy)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasQueryFilter(e => !e.IsDeleted);
            entity.HasIndex(e => e.UploadedBy);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure DocumentTag entity
        builder.Entity<DocumentTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TagName).IsRequired().HasMaxLength(100);

            entity.HasOne(e => e.Document)
                  .WithMany(d => d.Tags)
                  .HasForeignKey(e => e.DocumentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.TagName);
        });

        // Configure RefreshToken entity
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ApplicationUserId).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.ApplicationUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ApplicationUserId);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });

        // Student - ApplicationUser relationship
        builder.Entity<Student>()
            .HasOne(s => s.ApplicationUser)
            .WithMany()
            .HasForeignKey(s => s.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guardian - ApplicationUser relationship
        builder.Entity<Guardian>()
            .HasOne(g => g.ApplicationUser)
            .WithMany()
            .HasForeignKey(g => g.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Guardian-Student many-to-many
        builder.Entity<Guardian>()
            .HasMany(g => g.Students)
            .WithMany(s => s.Guardians);

        // BandStaff - ApplicationUser relationship
        builder.Entity<BandStaff>()
            .HasOne(bs => bs.ApplicationUser)
            .WithMany()
            .HasForeignKey(bs => bs.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // BandStaff default values
        builder.Entity<BandStaff>()
            .Property(bs => bs.CanViewStudents)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanRateStudents)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanSendOffers)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanManageEvents)
            .HasDefaultValue(false);

        builder.Entity<BandStaff>()
            .Property(bs => bs.CanManageStaff)
            .HasDefaultValue(false);

        // StudentGuardian configuration
        builder.Entity<StudentGuardian>(entity =>
        {
            entity.HasKey(sg => new { sg.StudentId, sg.GuardianUserId });

            entity.HasOne(sg => sg.Student)
                  .WithMany()
                  .HasForeignKey(sg => sg.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}