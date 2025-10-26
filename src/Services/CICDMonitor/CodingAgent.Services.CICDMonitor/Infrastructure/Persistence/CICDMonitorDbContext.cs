using CodingAgent.Services.CICDMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CodingAgent.Services.CICDMonitor.Infrastructure.Persistence;

/// <summary>
/// Database context for the CI/CD Monitor service.
/// </summary>
public class CICDMonitorDbContext : DbContext
{
    public CICDMonitorDbContext(DbContextOptions<CICDMonitorDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the Builds DbSet.
    /// </summary>
    public DbSet<Build> Builds => Set<Build>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Build>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.HasIndex(b => b.WorkflowRunId)
                .IsUnique();

            entity.HasIndex(b => new { b.Owner, b.Repository, b.CreatedAt });

            entity.Property(b => b.Owner)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(b => b.Repository)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(b => b.Branch)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.CommitSha)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(b => b.WorkflowName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(b => b.Conclusion)
                .HasMaxLength(50);

            entity.Property(b => b.WorkflowUrl)
                .IsRequired()
                .HasMaxLength(500);

            // Persist error messages as JSON (jsonb) for robustness
            entity.Property(b => b.ErrorMessages)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v) ?? new List<string>())
                .HasColumnType("jsonb");
        });
    }
}
