using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class WelcomeGate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WelcomeGateConfigs",
                columns: table => new
                {
                    GuildID = table.Column<ulong>(nullable: false),
                    RoleId = table.Column<ulong>(nullable: true),
                    Toggle = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WelcomeGateConfigs", x => x.GuildID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WelcomeGateConfigs");
        }
    }
}
