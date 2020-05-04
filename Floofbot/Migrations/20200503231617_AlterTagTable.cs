using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class AlterTagTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagsNext",
                columns: table => new
                {
                    TagId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagName = table.Column<string>(nullable: false, defaultValue: ""),
                    ServerId = table.Column<ulong>(nullable: false, defaultValue: 0ul),
                    UserId = table.Column<ulong>(nullable: false, defaultValue: 0ul),
                    TagContent = table.Column<string>(nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.Sql(@"INSERT INTO TagsNext (UserId, TagContent)
                SELECT UserID, Content
                FROM Tags");

            migrationBuilder.DropTable("Tags");
            migrationBuilder.RenameTable("TagsNext", null, "Tags", null);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TagsPrev",
                columns: table => new
                {
                    TagID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<ulong>(nullable: false, defaultValue: 0ul),
                    Content = table.Column<string>(nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagID);
                });

            migrationBuilder.Sql(@"INSERT INTO TagsPrev (UserID, Content)
                SELECT UserId, TagContent
                FROM Tags");

            migrationBuilder.DropTable("Tags");
            migrationBuilder.RenameTable("TagsPrev", null, "Tags", null);
        }
    }
}
