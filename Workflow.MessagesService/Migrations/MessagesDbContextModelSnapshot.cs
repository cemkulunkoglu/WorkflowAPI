using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Workflow.MessagesService.Persistence;

#nullable disable

namespace Workflow.MessagesService.Migrations;

[DbContext(typeof(MessagesDbContext))]
partial class MessagesDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "8.0.22")
            .HasAnnotation("Relational:MaxIdentifierLength", 64);

        modelBuilder.Entity("Workflow.MessagesService.Entities.InboxMessage", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreateDate")
                    .HasColumnType("datetime");

                b.Property<int>("EmployeeFromId")
                    .HasColumnType("int");

                b.Property<int>("EmployeeToId")
                    .HasColumnType("int");

                b.Property<string>("EmailFrom")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<string>("EmailTo")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<int>("FlowDesignsId")
                    .HasColumnType("int");

                b.Property<int>("FlowNodesId")
                    .HasColumnType("int");

                b.Property<string>("Subject")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<DateTime?>("UpdateDate")
                    .HasColumnType("datetime");

                b.HasKey("Id");

                b.ToTable("Inbox");
            });

        modelBuilder.Entity("Workflow.MessagesService.Entities.OutboxMessage", b =>
            {
                b.Property<int>("Id")
                    .ValueGeneratedOnAdd()
                    .HasColumnType("int");

                b.Property<DateTime>("CreateDate")
                    .HasColumnType("datetime");

                b.Property<int>("EmployeeFromId")
                    .HasColumnType("int");

                b.Property<int>("EmployeeToId")
                    .HasColumnType("int");

                b.Property<string>("EmailFrom")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<string>("EmailTo")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<int>("FlowDesignsId")
                    .HasColumnType("int");

                b.Property<int>("FlowNodesId")
                    .HasColumnType("int");

                b.Property<string>("Subject")
                    .IsRequired()
                    .HasColumnType("varchar(255)");

                b.Property<DateTime?>("UpdateDate")
                    .HasColumnType("datetime");

                b.HasKey("Id");

                b.ToTable("Outbox");
            });
    }
}
