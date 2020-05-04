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
    public class TagCommands : ModuleBase<SocketCommandContext>
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        private SqliteConnection dbConnection;

        public TagCommands()
        {
            dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder
            {
                DataSource = "botdata.db"
            }.ToString());
            dbConnection.Open();
        }

        [Command("add")]
        [Priority(0)]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task Add(
            [Summary("Tag name")] string tag,
            [Summary("Tag content")] [Remainder] string content = null)
        {
            try
            {
                if (content != null)
                {
                    Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                    tag = rgx.Replace(tag, "").ToLower();
                    string sql = @"INSERT into Tags (TagID,UserID, Content)
                            VALUES ($TagId, $UserID, $Content)";

                    string tagId = $"{tag.ToString()}:{Context.Guild.Id.ToString()}";
                    SqliteCommand command = new SqliteCommand(sql, dbConnection);
                    command.Parameters.Add(new SqliteParameter("$TagId", tagId));
                    command.Parameters.Add(new SqliteParameter("$UserID", Context.User.Id.ToString()));
                    command.Parameters.Add(new SqliteParameter("$Content", content));
                    var result = command.ExecuteScalar();

                    await SendEmbed(CreateDescriptionEmbed($"💾 Added Tag `{tag}`"));
                }
                else
                {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag add [name] [content]`"));
                }
            }
            catch (SqliteException)
            {
                await SendEmbed(CreateDescriptionEmbed($"💾 Tag `{tag}` Already Exists"));
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
        public async Task ListTags([Remainder] string content = null)
        {
            string guildId = $"{Context.Guild.Id}";
            string sql = $"SELECT tagID FROM Tags WHERE tagID LIKE '%:$guildId%'";
            SqliteCommand command = new SqliteCommand(sql, dbConnection);
            command.Parameters.Add(new SqliteParameter("$guildId", guildId));
            var result = command.ExecuteReader();

            List<string> tags = new List<string>();
            List<string> pages = new List<string>();

            while (result.Read())
            {
                tags.Add(result.GetString(0).Split(':')[0]);
            }

            tags = tags.OrderBy(x => x).ToList();
            int index = 0;
            for (int i = 1; i <= (tags.Count / 50) + 1; i++)
            {
                string text = "```glsl\n";
                int pagebreak = index;
                for (; index < pagebreak + 50; index++)
                {
                    if (index < tags.Count)
                    {
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

            if (Convert.ToInt32(command.ExecuteScalar()) > 0)
            {
                string delete = @"DELETE FROM Tags WHERE TagID = $TagId";
                command = new SqliteCommand(delete, dbConnection);
                command.Parameters.Add(new SqliteParameter("$TagId", tagId));
                command.ExecuteScalar();

                await SendEmbed(CreateDescriptionEmbed($"💾 Tag: `{tag}` Removed"));
            }
            else
            {
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
        public async Task GetTag([Summary("Tag name")] string tag = "")
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                string TagID = $"{tag}:{Context.Guild.Id}".ToLower();
                string sql = @"SELECT Content FROM Tags
                                Where TagID = $TagID";
                SqliteCommand command = new SqliteCommand(sql, dbConnection);
                command.Parameters.Add(new SqliteParameter("$TagID", TagID));
                var result = command.ExecuteScalar();

                if (result != null)
                {
                    string mentionless_tag = result.ToString().Replace("@", "[at]");

                    bool isImage = false;
                    if (Uri.IsWellFormedUriString(mentionless_tag, UriKind.RelativeOrAbsolute))
                    {
                        string ext = mentionless_tag.Split('.').Last().ToLower();
                        List<string> imageExtensions = new List<string> {
                            "jpg",
                            "png",
                            "jpeg",
                            "webp",
                            "gifv",
                            "gif",
                            "mp4"
                        };
                        isImage = imageExtensions.Contains(ext);
                    }

                    // tag found, so post it
                    if (isImage)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            Title = "💾  " + mentionless_tag,
                            Color = Color.Magenta
                        };
                        builder.WithImageUrl(mentionless_tag);
                        await SendEmbed(builder.Build());
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(mentionless_tag);
                    }
                }
                else
                {
                    // tag not found
                    await SendEmbed(CreateDescriptionEmbed($"💾 Could not find Tag: `{tag}`"));
                }
            }
            else
            {
                // no tag given
                await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag [name]`"));
            }
        }

        private Embed CreateDescriptionEmbed(string description)
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Description = description,
                Color = EMBED_COLOR
            };
            return builder.Build();
        }

        private Task SendEmbed(Embed embed)
        {
            return Context.Channel.SendMessageAsync("", false, embed);
        }
    }
}
