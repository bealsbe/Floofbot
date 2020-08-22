using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class RaidProtection : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RaidProtectionConfigs",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    ModChannelId = table.Column<ulong>(nullable: true),
                    ModRoleId = table.Column<ulong>(nullable: true),
                    MutedRoleId = table.Column<ulong>(nullable: true),
                    ExceptionRoleId = table.Column<ulong>(nullable: true),
                    BanOffenders = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidProtectionConfigs", x => x.ServerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidProtectionConfigs");
        }
    }
}
