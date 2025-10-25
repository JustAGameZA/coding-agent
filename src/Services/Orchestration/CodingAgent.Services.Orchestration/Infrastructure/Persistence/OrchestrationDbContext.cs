using CodingAgent.Services.Orchestration.Domain.Entities;
using CodingAgent.Services.Orchestration.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Orchestration.Infrastructure.Persistence;

public class OrchestrationDbContext : DbContext
{
    public OrchestrationDbContext(DbContextOptions<OrchestrationDbContext> options)
        : base(options)
    {
    }

    public DbSet<CodingTask> Tasks => Set<CodingTask>();
    public DbSet<TaskExecution> Executions => Set<TaskExecution>();
    public DbSet<ExecutionResult> ExecutionResults => Set<ExecutionResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set schema for all tables in this context
        modelBuilder.HasDefaultSchema("orchestration");

        // Configure CodingTask entity
        modelBuilder.Entity<CodingTask>(entity =>
        {
            entity.ToTable("tasks");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // We generate GUIDs in the entity

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(10000);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.Complexity)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.StartedAt)
                .IsRequired(false);

            entity.Property(e => e.CompletedAt)
                .IsRequired(false);

            // Configure one-to-many relationship with Executions
            entity.HasMany(t => t.Executions)
                .WithOne()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.UserId, e.Status });
        });

        // Configure TaskExecution entity
        modelBuilder.Entity<TaskExecution>(entity =>
        {
            entity.ToTable("executions");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // We generate GUIDs in the entity

            entity.Property(e => e.TaskId)
                .IsRequired();

            entity.Property(e => e.Strategy)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.ModelUsed)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<string>(); // Store enum as string

            entity.Property(e => e.ErrorMessage)
                .IsRequired(false)
                .HasMaxLength(5000);

            entity.Property(e => e.StartedAt)
                .IsRequired();

            entity.Property(e => e.CompletedAt)
                .IsRequired(false);

            // Configure one-to-one relationship with ExecutionResult
            entity.HasOne(e => e.Result)
                .WithOne(r => r.Execution)
                .HasForeignKey<ExecutionResult>(r => r.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.StartedAt);
            entity.HasIndex(e => e.Status);
        });

        // Configure ExecutionResult entity
        modelBuilder.Entity<ExecutionResult>(entity =>
        {
            entity.ToTable("execution_results");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .ValueGeneratedNever(); // We generate GUIDs in the entity

            entity.Property(e => e.ExecutionId)
                .IsRequired();

            entity.Property(e => e.Success)
                .IsRequired();

            entity.Property(e => e.Changes)
                .IsRequired(false)
                .HasMaxLength(50000); // Large text for code changes

            entity.Property(e => e.TokensUsed)
                .IsRequired();

            entity.Property(e => e.CostUSD)
                .IsRequired()
                .HasPrecision(18, 6); // Precise cost tracking

            entity.Property(e => e.ErrorDetails)
                .IsRequired(false)
                .HasMaxLength(10000);

            entity.Property(e => e.FilesChanged)
                .IsRequired();

            entity.Property(e => e.LinesAdded)
                .IsRequired();

            entity.Property(e => e.LinesRemoved)
                .IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.ExecutionId)
                .IsUnique();
            entity.HasIndex(e => e.Success);
        });
    }
}
