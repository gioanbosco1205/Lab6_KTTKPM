using Microsoft.EntityFrameworkCore;
using ChatService.Models;

namespace ChatService.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<Agent> Agents { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SenderId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SenderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ReceiverId).HasMaxLength(100);
            entity.Property(e => e.ReceiverName).HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.MessageType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsRead).IsRequired().HasDefaultValue(false);

            // Indexes for better query performance
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ReceiverId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.MessageType);
            entity.HasIndex(e => new { e.SenderId, e.ReceiverId });
        });

        // Configure Agent
        modelBuilder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.AgentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.LastSeen).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Unique constraint on AgentId
            entity.HasIndex(e => e.AgentId).IsUnique();

            // Configure relationships
            entity.HasMany(e => e.SentMessages)
                  .WithOne()
                  .HasForeignKey(m => m.SenderId)
                  .HasPrincipalKey(a => a.AgentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.ReceivedMessages)
                  .WithOne()
                  .HasForeignKey(m => m.ReceiverId)
                  .HasPrincipalKey(a => a.AgentId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Message
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Payload).IsRequired();

            // Index for better query performance on Type
            entity.HasIndex(e => e.Type);
        });

        // Configure OutboxMessage
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("outbox_messages");
            
            entity.Property(e => e.Type).IsRequired().HasMaxLength(500);
            entity.Property(e => e.JsonPayload).IsRequired().HasColumnName("json_payload");
            entity.Property(e => e.CreatedAt).IsRequired().HasColumnName("created_at");
            entity.Property(e => e.IsProcessed).IsRequired().HasColumnName("is_processed").HasDefaultValue(false);
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");

            // Indexes for better query performance
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsProcessed);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.IsProcessed, e.CreatedAt });
        });
    }
}