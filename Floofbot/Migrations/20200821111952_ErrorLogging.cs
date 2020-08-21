using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class ErrorLogging : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ErrorLoggingConfigs",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false),
                    ChannelId = table.Column<ulong?>(nullable: true),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLoggingConfigs", x => x.ServerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ErrorLoggingConfigs");
        }
    }
}
