using CodingAgent.Services.CICDMonitor.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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

    public DbSet<BuildFailure> BuildFailures => Set<BuildFailure>();
    public DbSet<FixAttempt> FixAttempts => Set<FixAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set schema for all tables in this context
        modelBuilder.HasDefaultSchema("cicd_monitor");

        // Configure BuildFailure entity
        modelBuilder.Entity<BuildFailure>(entity =>
        {
            entity.ToTable("build_failures");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // We generate GUIDs manually

            entity.Property(e => e.Repository)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Branch)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.CommitSha)
                .IsRequired()
                .HasMaxLength(40);

            entity.Property(e => e.ErrorMessage)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.ErrorLog)
                .HasMaxLength(50000);

            entity.Property(e => e.WorkflowName)
                .HasMaxLength(200);

            entity.Property(e => e.JobName)
                .HasMaxLength(200);

            entity.Property(e => e.ErrorPattern)
                .HasMaxLength(200);

            entity.Property(e => e.FailedAt)
                .IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.Repository);
            entity.HasIndex(e => e.FailedAt);
            entity.HasIndex(e => e.ErrorPattern);
        });

        // Configure FixAttempt entity
        modelBuilder.Entity<FixAttempt>(entity =>
        {
            entity.ToTable("fix_attempts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // We generate GUIDs manually

            entity.Property(e => e.BuildFailureId)
                .IsRequired();

            entity.Property(e => e.TaskId)
                .IsRequired();

            entity.Property(e => e.Repository)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.ErrorMessage)
                .IsRequired()
                .HasMaxLength(5000);

            entity.Property(e => e.ErrorPattern)
                .HasMaxLength(200);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.AttemptedAt)
                .IsRequired();

            entity.Property(e => e.CompletedAt)
                .IsRequired(false);

            entity.Property(e => e.PullRequestNumber)
                .IsRequired(false);

            entity.Property(e => e.PullRequestUrl)
                .HasMaxLength(500);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(2000);

            // Configure relationship with BuildFailure
            entity.HasOne(f => f.BuildFailure)
                .WithMany()
                .HasForeignKey(f => f.BuildFailureId)
                .OnDelete(DeleteBehavior.Restrict);

            // Indexes for common queries
            entity.HasIndex(e => e.TaskId).IsUnique();
            entity.HasIndex(e => e.BuildFailureId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.ErrorPattern);
            entity.HasIndex(e => e.AttemptedAt);
        });
    }
}
