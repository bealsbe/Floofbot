using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class AlterTagTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagName",
                table: "Tags",
                nullable: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagName",
                table: "Tags");
        }
    }
}
