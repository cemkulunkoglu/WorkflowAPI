using Microsoft.EntityFrameworkCore;
using MessagesService.Entities;

namespace MessagesService.Data;

public class MessagesDbContext : DbContext
{
    public MessagesDbContext(DbContextOptions<MessagesDbContext> options) : base(options) { }

    public DbSet<InboxMessage> Inbox => Set<InboxMessage>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<MessageTemplate> MessageTemplates => Set<MessageTemplate>();
    public DbSet<MessageTemplateField> MessageTemplateFields => Set<MessageTemplateField>();

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

        // MessageTemplates
        modelBuilder.Entity<MessageTemplate>(e =>
        {
            e.ToTable("MessageTemplates");
            e.HasKey(x => x.TemplateId);

            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Subject).HasMaxLength(255);

            e.Property(x => x.CreateDate).HasColumnType("datetime");
            e.Property(x => x.UpdateDate).HasColumnType("datetime");

            e.HasMany(x => x.Fields)
             .WithOne(x => x.Template)
             .HasForeignKey(x => x.TemplateId);
        });

        // MessageTemplateFields
        modelBuilder.Entity<MessageTemplateField>(e =>
        {
            e.ToTable("MessageTemplateFields");
            e.HasKey(x => x.FieldId);

            e.Property(x => x.DynamicField).HasMaxLength(255);
            e.Property(x => x.SystemField).HasMaxLength(255);
        });

    }
}
