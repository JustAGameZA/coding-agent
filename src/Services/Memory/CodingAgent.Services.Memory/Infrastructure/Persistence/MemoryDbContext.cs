using CodingAgent.Services.Memory.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Memory.Infrastructure.Persistence;

/// <summary>
/// Database context for Memory service
/// </summary>
public class MemoryDbContext : DbContext
{
    public MemoryDbContext(DbContextOptions<MemoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Episode> Episodes => Set<Episode>();
    public DbSet<SemanticMemory> SemanticMemories => Set<SemanticMemory>();
    public DbSet<Procedure> Procedures => Set<Procedure>();
    public DbSet<MemoryAssociation> MemoryAssociations => Set<MemoryAssociation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set schema for all tables in this context
        modelBuilder.HasDefaultSchema("memory");

        // Episode configuration
        modelBuilder.Entity<Episode>(entity =>
        {
            entity.ToTable("episodes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.TaskId)
                .HasColumnName("task_id");

            entity.Property(e => e.ExecutionId)
                .HasColumnName("execution_id");

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .HasColumnName("timestamp")
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasColumnName("event_type")
                .IsRequired()
                .HasMaxLength(50);

            // JSONB columns for complex data
            entity.Property(e => e.Context)
                .HasColumnName("context")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.Outcome)
                .HasColumnName("outcome")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.LearnedPatterns)
                .HasColumnName("learned_patterns")
                .HasColumnType("text[]");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.TaskId);
            entity.HasIndex(e => e.ExecutionId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.EventType);
        });

        // SemanticMemory configuration
        modelBuilder.Entity<SemanticMemory>(entity =>
        {
            entity.ToTable("semantic_memories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.ContentType)
                .HasColumnName("content_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired();

            // Note: Vector embedding will be stored using pgvector extension
            // For now, we'll use a float array; pgvector migration will be added separately
            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(1536)"); // OpenAI/LLM embedding dimension

            entity.Property(e => e.Metadata)
                .HasColumnName("metadata")
                .HasColumnType("jsonb");

            entity.Property(e => e.SourceEpisodeId)
                .HasColumnName("source_episode_id");

            entity.Property(e => e.ConfidenceScore)
                .HasColumnName("confidence_score")
                .HasDefaultValue(1.0f);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.ContentType);
            entity.HasIndex(e => e.SourceEpisodeId);
            entity.HasIndex(e => e.ConfidenceScore);

            // Foreign key to Episode
            entity.HasOne<Episode>()
                .WithMany()
                .HasForeignKey(e => e.SourceEpisodeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Procedure configuration
        modelBuilder.Entity<Procedure>(entity =>
        {
            entity.ToTable("procedures");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.ProcedureName)
                .HasColumnName("procedure_name")
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasColumnName("description");

            entity.Property(e => e.ContextPattern)
                .HasColumnName("context_pattern")
                .HasColumnType("jsonb");

            // Steps stored as JSONB array
            entity.Property(e => e.Steps)
                .HasColumnName("steps")
                .HasColumnType("jsonb")
                .IsRequired();

            entity.Property(e => e.SuccessRate)
                .HasColumnName("success_rate")
                .HasDefaultValue(0.0f);

            entity.Property(e => e.AvgExecutionTime)
                .HasColumnName("avg_execution_time");

            entity.Property(e => e.LastUsedAt)
                .HasColumnName("last_used_at");

            entity.Property(e => e.UsageCount)
                .HasColumnName("usage_count")
                .HasDefaultValue(0);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.ProcedureName);
            entity.HasIndex(e => e.SuccessRate);
            entity.HasIndex(e => e.UsageCount);
        });

        // MemoryAssociation configuration
        modelBuilder.Entity<MemoryAssociation>(entity =>
        {
            entity.ToTable("memory_associations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever();

            entity.Property(e => e.SourceMemoryId)
                .HasColumnName("source_memory_id")
                .IsRequired();

            entity.Property(e => e.SourceMemoryType)
                .HasColumnName("source_memory_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.TargetMemoryId)
                .HasColumnName("target_memory_id")
                .IsRequired();

            entity.Property(e => e.TargetMemoryType)
                .HasColumnName("target_memory_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.AssociationType)
                .HasColumnName("association_type")
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Strength)
                .HasColumnName("strength")
                .HasDefaultValue(1.0f);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            // Indexes
            entity.HasIndex(e => e.SourceMemoryId);
            entity.HasIndex(e => e.TargetMemoryId);
            entity.HasIndex(e => e.AssociationType);
        });
    }
}

