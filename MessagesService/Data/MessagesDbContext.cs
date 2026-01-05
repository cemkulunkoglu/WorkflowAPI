using Microsoft.EntityFrameworkCore;
using MessagesService.Entities;

namespace MessagesService.Data;

public class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options) { }

    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Inbox
        modelBuilder.Entity<InboxMessage>(e =>
        {
            e.ToTable("Inbox");
            e.HasKey(x => x.Id);

            e.Property(x => x.EmailTo).HasMaxLength(255);
            e.Property(x => x.EmailFrom).HasMaxLength(255);
            e.Property(x => x.Subject).HasMaxLength(255);

            e.Property(x => x.CreateDate).HasColumnType("datetime");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");
        });

        // Outbox
        modelBuilder.Entity<OutboxMessage>(e =>
        {
            e.ToTable("Outbox");
            e.HasKey(x => x.Id);

            e.Property(x => x.EmailTo).HasMaxLength(255);
            e.Property(x => x.EmailFrom).HasMaxLength(255);
            e.Property(x => x.Subject).HasMaxLength(255);

            e.Property(x => x.CreateDate).HasColumnType("datetime");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");
        });
    }
}
