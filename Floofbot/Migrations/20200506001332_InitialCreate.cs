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
                name: "FilterChannelWhitelists",
                columns: table => new
                {
                    ChannelId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    ServerId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ServerId = table.Column<ulong>(nullable: false),
                    Word = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilteredWords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogConfigs",
                columns: table => new
                {
                    ServerId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
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
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogConfigs", x => x.ServerId);
                });

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

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TagName = table.Column<string>(nullable: true),
                    ServerId = table.Column<ulong>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false),
                    TagContent = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    Forgiven = table.Column<bool>(nullable: false),
                    ForgivenBy = table.Column<ulong>(nullable: false),
                    GuildId = table.Column<ulong>(nullable: false),
                    ModeratorId = table.Column<ulong>(nullable: false),
                    Moderator = table.Column<string>(nullable: true),
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
                name: "FilterChannelWhitelists");

            migrationBuilder.DropTable(
                name: "FilterConfigs");

            migrationBuilder.DropTable(
                name: "FilteredWords");

            migrationBuilder.DropTable(
                name: "LogConfigs");

            migrationBuilder.DropTable(
                name: "NicknameAlertConfigs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Warnings");
        }
    }
}
