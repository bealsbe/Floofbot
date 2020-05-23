using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
                    MuteRoleId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilterChannelWhitelists",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChannelId = table.Column<decimal>(nullable: false),
                    ServerId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterChannelWhitelists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilterConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FilterConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FilteredWords",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
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
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
                    MessageUpdatedChannel = table.Column<decimal>(nullable: false),
                    MessageDeletedChannel = table.Column<decimal>(nullable: false),
                    UserBannedChannel = table.Column<decimal>(nullable: false),
                    UserUnbannedChannel = table.Column<decimal>(nullable: false),
                    UserJoinedChannel = table.Column<decimal>(nullable: false),
                    UserLeftChannel = table.Column<decimal>(nullable: false),
                    MemberUpdatesChannel = table.Column<decimal>(nullable: false),
                    UserKickedChannel = table.Column<decimal>(nullable: false),
                    UserMutedChannel = table.Column<decimal>(nullable: false),
                    UserUnmutedChannel = table.Column<decimal>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NicknameAlertConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
                    Channel = table.Column<decimal>(nullable: false),
                    IsOn = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NicknameAlertConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TagConfigs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServerId = table.Column<decimal>(nullable: false),
                    TagUpdateRequiresAdmin = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    TagId = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TagName = table.Column<string>(nullable: true),
                    ServerId = table.Column<decimal>(nullable: false),
                    UserId = table.Column<decimal>(nullable: false),
                    TagContent = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.TagId);
                });

            migrationBuilder.CreateTable(
                name: "UserAssignableRoles",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<decimal>(nullable: false),
                    ServerId = table.Column<decimal>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAssignableRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Warnings",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    Forgiven = table.Column<bool>(nullable: false),
                    ForgivenBy = table.Column<decimal>(nullable: false),
                    GuildId = table.Column<decimal>(nullable: false),
                    ModeratorId = table.Column<decimal>(nullable: false),
                    Moderator = table.Column<string>(nullable: true),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<decimal>(nullable: false)
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
                name: "TagConfigs");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "UserAssignableRoles");

            migrationBuilder.DropTable(
                name: "Warnings");
        }
    }
}
