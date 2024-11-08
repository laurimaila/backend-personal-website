using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace backend.Models;

public interface IMessagingContext
{
    DbSet<Message> Messages { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class MessagingContext : DbContext, IMessagingContext
{
    public MessagingContext(DbContextOptions<MessagingContext> options)
        : base(options)
    {
    }

    public DbSet<Message> Messages => Set<Message>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            entity.Property(e => e.Content)
                .IsRequired()
                .HasColumnName("content");

            entity.Property(e => e.Creator)
                .IsRequired()
                .HasColumnName("creator_name")
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}

[Table("messages")]
public class Message
{
    [Key] [Column("id")] public int Id { get; set; }

    [Required] [Column("content")] public string Content { get; set; } = string.Empty;

    [Required]
    [Column("creator")]
    [MaxLength(100)]
    public string Creator { get; set; } = string.Empty;

    [Column("created_at")] public DateTime CreatedAt { get; set; }
}
