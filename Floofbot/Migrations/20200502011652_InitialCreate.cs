using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Floofbot.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminConfig",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    MuteRoleId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminConfig", x => x.ServerId);
                });

            migrationBuilder.CreateTable(
                name: "LogConfigs",
                columns: table => new
                {
                    ServerID = table.Column<ulong>(nullable: false),
                    MessageUpdatedChannel = table.Column<ulong>(nullable: false),
                    MessageDeletedChannel = table.Column<ulong>(nullable: false),
                    UserBannedChannel = table.Column<ulong>(nullable: false),
                    UserUnbannedChannel = table.Column<ulong>(nullable: false),
                    UserJoinedChannel = table.Column<ulong>(nullable: false),
                    UserLeftChannel = table.Column<ulong>(nullable: false),
                    MemberUpdatesChannel = table.Column<ulong>(nullable: false),
                    UserKickedChannel = table.Column<ulong>(nullable: false),
                    UserMutedChannel = table.Column<ulong>(nullable: false),
                    UserUnmutedChannel = table.Column<ulong>(nullable: false),
                    IsOn = table.Column<bool>(type: "INT", nullable: false)
                },
                constraints: table =>
                {
                    
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagID = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserID = table.Column<ulong>(nullable: false),
                    Content = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagID);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    Forgiven = table.Column<bool>(nullable: false),
                    ForgivenBy = table.Column<ulong>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    Moderator = table.Column<ulong>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Warnings", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminConfig");

            migrationBuilder.DropTable(
                name: "LogConfigs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Warnings");
        }
    }
}
