using DocumentManagement.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DocumentManagement.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentTag> DocumentTags { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }

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
            entity.Property(e => e.UserId).IsRequired();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);
        });

        // Configure ApplicationUser
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
        });
    }
}