using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class AddNicknameAlert : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NicknameAlertConfigs",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Channel = table.Column<ulong>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NicknameAlertConfigs", x => x.ServerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NicknameAlertConfigs");
        }
    }
}
