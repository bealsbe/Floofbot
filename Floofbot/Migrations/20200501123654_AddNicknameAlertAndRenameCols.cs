using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class AddNicknameAlertAndRenameCols : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "Tags",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "TagID",
                table: "Tags",
                newName: "TagId");

            migrationBuilder.RenameColumn(
                name: "ServerID",
                table: "LogConfigs",
                newName: "ServerId");

            migrationBuilder.AlterColumn<ulong>(
                name: "Id",
                table: "Warnings",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<ulong>(
                name: "ServerId",
                table: "LogConfigs",
                nullable: false,
                oldClrType: typeof(ulong),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsOn",
                table: "LogConfigs",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "INT");

            migrationBuilder.AddPrimaryKey(
                name: "PK_LogConfigs",
                table: "LogConfigs",
                column: "ServerId");

            migrationBuilder.CreateTable(
                name: "NicknameAlerts",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Channel = table.Column<ulong>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NicknameAlerts", x => x.ServerId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NicknameAlerts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_LogConfigs",
                table: "LogConfigs");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Tags",
                newName: "UserID");

            migrationBuilder.RenameColumn(
                name: "TagId",
                table: "Tags",
                newName: "TagID");

            migrationBuilder.RenameColumn(
                name: "ServerId",
                table: "LogConfigs",
                newName: "ServerID");

            migrationBuilder.AlterColumn<ulong>(
                name: "Id",
                table: "Warnings",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong))
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsOn",
                table: "LogConfigs",
                type: "INT",
                nullable: false,
                oldClrType: typeof(bool));

            migrationBuilder.AlterColumn<ulong>(
                name: "ServerID",
                table: "LogConfigs",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(ulong))
                .OldAnnotation("Sqlite:Autoincrement", true);
        }
    }
}
