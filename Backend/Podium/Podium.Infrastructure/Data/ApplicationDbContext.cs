using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Podium.Core.Entities;
using Podium.Core.Interfaces;
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
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<BandBudget> BandBudgets { get; set; }
    public DbSet<ScholarshipOffer> ScholarshipOffers { get; set; }
    public DbSet<SavedSearch> SavedSearches { get; set; }
    public DbSet<SearchAlert> SearchAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // --- Global Query Filters for Soft Delete ---
        builder.Entity<Student>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<ScholarshipOffer>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Video>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Document>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<BandEvent>().HasQueryFilter(e => !e.IsDeleted);
        builder.Entity<Band>().HasQueryFilter(e => !e.IsDeleted);

    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<IAuditableEntity>();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}