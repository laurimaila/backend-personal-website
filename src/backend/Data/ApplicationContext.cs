using backend.Data.Entities;

using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public interface IApplicationContext
{
    DbSet<Message> Messages { get; }
    DbSet<User> Users { get; }
    DbSet<MessageReaction> MessageReactions { get; }
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
    public DbSet<MessageReaction> MessageReactions => Set<MessageReaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(e => e.CreatorUser)
                .WithMany()
                .HasForeignKey(e => e.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsAdmin)
                .HasDefaultValue(false);

            entity.Property(e => e.NameColor)
                .HasDefaultValue("#ffffff");

            entity.HasIndex(e => e.Username)
                .IsUnique();
        });

        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.HasOne(e => e.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.MessageId, e.UserId })
                .IsUnique();
        });
    }
}
