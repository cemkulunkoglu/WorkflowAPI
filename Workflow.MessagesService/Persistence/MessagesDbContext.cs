using Microsoft.EntityFrameworkCore;
using Workflow.MessagesService.Entities;

namespace Workflow.MessagesService.Persistence;

public class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options)
    {
    }

    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>().ToTable("Inbox");
        modelBuilder.Entity<OutboxMessage>().ToTable("Outbox");
    }
}
