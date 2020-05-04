using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class AddFilterTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FilterChannelWhitelists",
                columns: table => new
                {
                    ChannelId = table.Column<ulong>(nullable: false),
                    ServerId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterChannelWhitelists", x => x.ChannelId);
                });

            migrationBuilder.CreateTable(
                name: "FilterConfigs",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterConfigs", x => x.ServerId);
                });

            migrationBuilder.CreateTable(
                name: "FilteredWords",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<ulong>(nullable: false),
                    Word = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilteredWords", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FilterChannelWhitelists");

            migrationBuilder.DropTable(
                name: "FilterConfigs");

            migrationBuilder.DropTable(
                name: "FilteredWords");
        }
    }
}
