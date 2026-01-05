using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workflow.MessagesService.Migrations;

public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Inbox",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                FlowDesignsId = table.Column<int>(type: "int", nullable: false),
                FlowNodesId = table.Column<int>(type: "int", nullable: false),
                EmployeeToId = table.Column<int>(type: "int", nullable: false),
                EmployeeFromId = table.Column<int>(type: "int", nullable: false),
                EmailTo = table.Column<string>(type: "varchar(255)", nullable: false),
                EmailFrom = table.Column<string>(type: "varchar(255)", nullable: false),
                Subject = table.Column<string>(type: "varchar(255)", nullable: false),
                CreateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Inbox", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Outbox",
            columns: table => new
            {
                Id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                FlowDesignsId = table.Column<int>(type: "int", nullable: false),
                FlowNodesId = table.Column<int>(type: "int", nullable: false),
                EmployeeToId = table.Column<int>(type: "int", nullable: false),
                EmployeeFromId = table.Column<int>(type: "int", nullable: false),
                EmailTo = table.Column<string>(type: "varchar(255)", nullable: false),
                EmailFrom = table.Column<string>(type: "varchar(255)", nullable: false),
                Subject = table.Column<string>(type: "varchar(255)", nullable: false),
                CreateDate = table.Column<DateTime>(type: "datetime", nullable: false),
                UpdateDate = table.Column<DateTime>(type: "datetime", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Outbox", x => x.Id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Inbox");

        migrationBuilder.DropTable(
            name: "Outbox");
    }
}
