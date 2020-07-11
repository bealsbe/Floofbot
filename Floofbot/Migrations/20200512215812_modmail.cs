using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class modmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModMails",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false),
                    ModRoleId = table.Column<ulong>(nullable: true),
                    ChannelId = table.Column<ulong>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModMails", x => x.ServerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ModMails");
        }
    }
}
