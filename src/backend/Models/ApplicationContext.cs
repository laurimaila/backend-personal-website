using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace backend.Models;

public interface IApplicationContext
{
    DbSet<Message> Messages { get; }
    DbSet<User> Users { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class ApplicationContext : DbContext, IApplicationContext
{
    public ApplicationContext(DbContextOptions<ApplicationContext> options)
        : base(options)
    {
    }

    public DbSet<Message> Messages => Set<Message>();
    public DbSet<User> Users => Set<User>();

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

            entity.Property(e => e.ModifiedAt)
                .HasColumnName("modified_at");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .UseIdentityColumn();

            entity.Property(e => e.Username)
                .IsRequired()
                .HasColumnName("username")
                .HasMaxLength(255);

            entity.Property(e => e.PasswordHash)
                .IsRequired()
                .HasColumnName("password_hash")
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.LastLogin)
                .HasColumnName("last_login");

            // Unique constraint for username
            entity.HasIndex(e => e.Username)
                .IsUnique();
        });
    }
}

[Table("messages")]
public class Message
{
    [Key][Column("id")] public int Id { get; set; }

    [Required][Column("content")] public string Content { get; set; } = string.Empty;

    [Required]
    [Column("creator")]
    [MaxLength(100)]
    public string Creator { get; set; } = string.Empty;

    [Column("created_at")] public DateTime CreatedAt { get; set; }
    [Column("modified_at")] public DateTime? ModifiedAt { get; set; }
}

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("username")]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("last_login")]
    public DateTime? LastLogin { get; set; }
}
