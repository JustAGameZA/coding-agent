using CodingAgent.Services.Chat.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodingAgent.Services.Chat.Infrastructure.Persistence;

/// <summary>
/// Database context for Chat service
/// </summary>
public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options)
    {
    }

    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set schema for all tables in this context
        modelBuilder.HasDefaultSchema("chat");

        // Conversation configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever(); // We generate GUIDs in the entity

            entity.Property(e => e.Title).HasColumnName("title").IsRequired().HasMaxLength(500);
            entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").IsRequired();
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // One-to-many relationship with Messages
            entity.HasMany(e => e.Messages)
                .WithOne()
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedNever(); // We generate GUIDs in the entity

            entity.Property(e => e.ConversationId).HasColumnName("conversation_id").IsRequired();
            entity.Property(e => e.Content).HasColumnName("content").IsRequired().HasMaxLength(50000); // Allow long messages
            entity.Property(e => e.Role).HasColumnName("role").IsRequired().HasConversion<string>(); // Store enum as string
            entity.Property(e => e.SentAt).HasColumnName("sent_at").IsRequired();

            // Indexes for common queries
            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.SentAt);
        });
    }
}
