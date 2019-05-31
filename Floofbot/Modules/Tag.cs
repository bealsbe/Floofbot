using Discord;
using Discord.Commands;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot
{
    public partial class Utility
    {
        [Group("tag")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(Discord.GuildPermission.AttachFiles)]
        public class Tag : ModuleBase<SocketCommandContext>
        {
            SqliteConnection dbConnection;
            List<string> pages;


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
            public async Task Add(string Tag, [Remainder] string Content = null)
            {
                try {
                    if (Content != null) {
                        Regex rgx = new Regex("[^a-zA-Z0-9 -]");
                        Tag = rgx.Replace(Tag, "").ToLower();
                        string sql = @"INSERT into Tags (TagID,UserID, Content)
                               VALUES ($TagId, $UserID, $Content)";

                        SqliteCommand command = new SqliteCommand(sql, dbConnection);
                        command.Parameters.Add(new SqliteParameter("$TagId", Tag.ToString() + ":" + Context.Guild.Id.ToString()));
                        command.Parameters.Add(new SqliteParameter("$UserID", Context.User.Id.ToString()));
                        command.Parameters.Add(new SqliteParameter("$Content", Content));

                        var result = command.ExecuteScalar();

                        EmbedBuilder builder = new EmbedBuilder() {
                            Description = $"💾 Added Tag `{Tag}`",
                            Color = Color.Magenta
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());

                    }
                    else {
                        EmbedBuilder builder = new EmbedBuilder() {
                            Description = $"💾 Usage: `tag add [name] [content]`",
                            Color = Color.Magenta
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                }
                catch (SqliteException) {
                    EmbedBuilder builder = new EmbedBuilder() {
                        Description = $"💾 Tag `{Tag}` Alright Exists",
                        Color = Color.Magenta
                    };
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }

            }

            [Command("add")]
            [Priority(1)]
            public async Task Add()
            {
                EmbedBuilder builder = new EmbedBuilder() {
                    Description = $"💾 Useage: `tag add [name] [content]`",
                    Color = Color.Magenta
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());

            }

            //TODO: Fix me
            [Command("list")]
            [RequireUserPermission(GuildPermission.AttachFiles)]
            public async Task Listtags([Remainder] string Content = null)
            {
                string sql = $"SELECT TAGID FROM Tags WHERE TagID LIKE '%:{Context.Guild.Id}%'";
                SqliteCommand command = new SqliteCommand(sql, dbConnection);
                var result = command.ExecuteReader();

                List<string> tags = new List<string>();
                pages = new List<string>();

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
            [RequireUserPermission(GuildPermission.ManageMessages)]
            public async Task Remove(string tag)
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
                    EmbedBuilder builder = new EmbedBuilder() {
                        Description = $"💾 Tag: `{tag}` Removed",
                        Color = Color.Magenta
                    };

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                else {
                    EmbedBuilder builder = new EmbedBuilder() {
                        Description = $"💾 Could not find Tag: `{tag}`",
                        Color = Color.Magenta
                    };
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            [Command("remove")]
            [Priority(1)]
            public async Task Remove()
            {
                EmbedBuilder builder = new EmbedBuilder() {
                    Description = $"💾 Useage: `tag remove [name]`",
                    Color = Color.Magenta
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());

            }

            [Command]
            [RequireUserPermission(GuildPermission.AttachFiles)]
            public async Task GetTag(string Tag)
            {
                if (Tag != null) {
                    string TagID = $"{Tag}:{Context.Guild.Id}".ToLower();
                    string sql = @"SELECT Content FROM Tags
                                 Where TagID = $TagID";
                    SqliteCommand command = new SqliteCommand(sql, dbConnection);
                    command.Parameters.Add(new SqliteParameter("$TagID", TagID));
                    var result = command.ExecuteScalar();

                    if (result != null) {
                        string tag = result.ToString().Replace("@", "[at]");

                        EmbedBuilder builder = new EmbedBuilder() {
                            Title = "💾  " + Tag,
                            Color = Color.Magenta
                        };

                        bool isImage = false;
                        if (Uri.IsWellFormedUriString(tag, UriKind.RelativeOrAbsolute)) {
                            string ext = tag.Split('.').Last().ToLower();
                            isImage = (
                               ext == "jpg" ||
                               ext == "png" ||
                               ext == "jpeg" |
                               ext == "webp" ||
                               ext == "gifv" ||
                               ext == "gif" ||
                               ext == "mp4");
                        }
                        if (isImage) {
                            builder.WithImageUrl(tag);
                            await Context.Channel.SendMessageAsync("", false, builder.Build());
                        }
                        else {
                            await Context.Channel.SendMessageAsync(tag);
                        }
                    }
                    else {
                        EmbedBuilder builder = new EmbedBuilder() {
                            Description = $"💾 Could not find Tag: `{Tag}`",
                            Color = Color.Magenta
                        };
                        await Context.Channel.SendMessageAsync("", false, builder.Build());
                    }
                }
                else {
                    await Context.Channel.SendMessageAsync("Usage: Entire String Here`");
                }
            }
        }
    }
}