using Discord;
using Discord.Commands;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Group("tag")]
    [Summary("Tag commands")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(Discord.GuildPermission.AttachFiles)]
    public class Tag : ModuleBase<SocketCommandContext>
    {
        private SqliteConnection dbConnection;
        private static readonly Discord.Color embedColor = Color.Magenta;

        public Tag()
        {
            dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = "botdata.db"
            }.ToString());
            dbConnection.Open();
        }

        [Command("add")]
        [Priority(0)]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task Add(
            [Summary("Tag name")] string Tag,
            [Summary("Tag content")] [Remainder] string Content = null)
        {
            try {
                if (Content != null) {
                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                    Tag = rgx.Replace(Tag, "").ToLower();
                    string sql = @"INSERT into Tags (TagID,UserID, Content)
                            VALUES ($TagId, $UserID, $Content)";

                    string tagId = $"{Tag.ToString()}:{Context.Guild.Id.ToString()}";
                    SqliteCommand command = new SqliteCommand(sql, dbConnection);
                    command.Parameters.Add(new SqliteParameter("$TagId", tagId));
                    command.Parameters.Add(new SqliteParameter("$UserID", Context.User.Id.ToString()));
                    command.Parameters.Add(new SqliteParameter("$Content", Content));
                    var result = command.ExecuteScalar();

                    await SendEmbed(CreateDescriptionEmbed($"💾 Added Tag `{Tag}`"));
                }
                else {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag add [name] [content]`"));
                }
            }
            catch (SqliteException) {
                await SendEmbed(CreateDescriptionEmbed($"💾 Tag `{Tag}` Already Exists"));
            }
        }

        [Command("add")]
        [Priority(1)]
        public async Task Add()
        {
            await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag add [name] [content]`"));
        }

        //TODO: Fix me
        [Command("list")]
        [Summary("Lists all tags")]
        public async Task ListTags([Remainder] string Content = null)
        {
            string guildId = $"{Context.Guild.Id}";
            string sql = $"SELECT tagID FROM Tags WHERE tagID LIKE '%:$guildId%'";
            SqliteCommand command = new SqliteCommand(sql, dbConnection);
            command.Parameters.Add(new SqliteParameter("$guildId", guildId));
            var result = command.ExecuteReader();

            List<string> tags = new List<string>();
            List<string> pages = new List<string>();

            while (result.Read()) {
                tags.Add(result.GetString(0).Split(':')[0]);
            }

            tags = tags.OrderBy(x => x).ToList();
            int index = 0;
            for (int i = 1; i <= (tags.Count / 50) + 1; i++) {
                string text = "```glsl\n";
                int pagebreak = index;
                for (; index < pagebreak + 50; index++) {
                    if (index < tags.Count) {
                        text += $"[{index}] - {tags[index].ToLower()}\n";
                    }
                }

                text += "\n```";
                pages.Add(text);
            };

            // await PagedReplyAsync(pages);
        }

        [Command("remove")]
        [Summary("Removes a tag")]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task Remove([Summary("Tag name")] string tag)
        {
            string tagId = $"{tag.ToString()}:{Context.Guild.Id.ToString()}";

            string select = @"SELECT COUNT(*) FROM Tags WHERE TagID = $TagId";
            SqliteCommand command = new SqliteCommand(select, dbConnection);
            command.Parameters.Add(new SqliteParameter("$TagId", tagId));

            if (Convert.ToInt32(command.ExecuteScalar()) > 0) {
                string delete = @"DELETE FROM Tags WHERE TagID = $TagId";
                command = new SqliteCommand(delete, dbConnection);
                command.Parameters.Add(new SqliteParameter("$TagId", tagId));
                command.ExecuteScalar();

                await SendEmbed(CreateDescriptionEmbed($"💾 Tag: `{tag}` Removed"));
            }
            else {
                await SendEmbed(CreateDescriptionEmbed($"💾 Could not find Tag: `{tag}`"));
            }
        }

        [Command("remove")]
        [Priority(1)]
        public async Task Remove()
        {
            await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag remove [name]`"));
        }

        [Command]
        [Summary("Displays a tag")]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task GetTag([Summary("Tag name")] string Tag = "")
        {
            if (!string.IsNullOrWhiteSpace(Tag)) {
                string TagID = $"{Tag}:{Context.Guild.Id}".ToLower();
                string sql = @"SELECT Content FROM Tags
                                Where TagID = $TagID";
                SqliteCommand command = new SqliteCommand(sql, dbConnection);
                command.Parameters.Add(new SqliteParameter("$TagID", TagID));
                var result = command.ExecuteScalar();

                if (result != null) {
                    string tag = result.ToString().Replace("@", "[at]");

                    bool isImage = false;
                    if (Uri.IsWellFormedUriString(tag, UriKind.RelativeOrAbsolute)) {
                        string ext = tag.Split('.').Last().ToLower();
                        isImage = (
                            ext == "jpg" ||
                            ext == "png" ||
                            ext == "jpeg" ||
                            ext == "webp" ||
                            ext == "gifv" ||
                            ext == "gif" ||
                            ext == "mp4");
                    }

                    // tag found, so post it
                    if (isImage) {
                        EmbedBuilder builder = new EmbedBuilder() {
                            Title = "💾  " + Tag,
                            Color = Color.Magenta
                        };
                        builder.WithImageUrl(tag);
                        await SendEmbed(builder.Build());
                    }
                    else {
                        await Context.Channel.SendMessageAsync(tag);
                    }
                }
                else {
                    // tag not found
                    await SendEmbed(CreateDescriptionEmbed($"💾 Could not find Tag: `{Tag}`"));
                }
            }
            else {
                // no tag given
                await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag [name]`"));
            }
        }

        private Embed CreateDescriptionEmbed(string description) {
            EmbedBuilder builder = new EmbedBuilder {
                Description = description,
                Color = embedColor
            };
            return builder.Build();
        }

        private Task SendEmbed(Embed embed) {
            return Context.Channel.SendMessageAsync("", false, embed);
        }
    }
}
