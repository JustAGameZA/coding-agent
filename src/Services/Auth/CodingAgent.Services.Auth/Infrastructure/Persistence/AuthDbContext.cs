using CodingAgent.Services.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Auth.Infrastructure.Persistence;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("auth");

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Roles)
                .IsRequired()
                .HasMaxLength(500)
                .HasDefaultValue("User");

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired();

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            // Unique constraints
            entity.HasIndex(e => e.Username)
                .IsUnique();

            entity.HasIndex(e => e.Email)
                .IsUnique();

            // Relationships
            entity.HasMany(u => u.Sessions)
                .WithOne(s => s.User)
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(u => u.ApiKeys)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Session configuration
        modelBuilder.Entity<Session>(entity =>
        {
            entity.ToTable("sessions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.RefreshTokenHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.IpAddress)
                .IsRequired()
                .HasMaxLength(45); // IPv6

            entity.Property(e => e.UserAgent)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            // Index for refresh token lookup
            entity.HasIndex(e => e.RefreshTokenHash);

            // Index for cleanup queries
            entity.HasIndex(e => new { e.IsRevoked, e.ExpiresAt });
        });

        // ApiKey configuration
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.ToTable("api_keys");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.KeyHash)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.ExpiresAt)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.IsRevoked)
                .IsRequired()
                .HasDefaultValue(false);

            // Index for API key lookup
            entity.HasIndex(e => e.KeyHash);

            // Index for cleanup queries
            entity.HasIndex(e => new { e.IsRevoked, e.ExpiresAt });
        });
    }
}
