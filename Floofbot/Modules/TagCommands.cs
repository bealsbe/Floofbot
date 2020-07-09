using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Tag commands")]
    [Discord.Commands.Name("Tag")]
    [Group("tag")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(Discord.GuildPermission.AttachFiles)]
    public class TagCommands : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;
        private static readonly int TAGS_PER_PAGE = 20;
        private static readonly List<string> SUPPORTED_IMAGE_EXTENSIONS = new List<string>
        {
            "jpg", "png", "jpeg", "webp", "gifv", "gif", "mp4"
        };

        private FloofDataContext _floofDb;

        public TagCommands(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }

        [Command("requireadmin")]
        [Summary("Set a server to require admin roles for adding or removing tags")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RequireAdmin([Summary("True/False")] string requireAdmin = "")
        {
            TagConfig config = await _floofDb.TagConfigs.AsQueryable()
                .Where(config => config.ServerId == Context.Guild.Id).FirstOrDefaultAsync();

            bool parsedRequireAdmin;
            if (!bool.TryParse(requireAdmin, out parsedRequireAdmin))
            {
                await SendEmbed(CreateDescriptionEmbed($"Usage: `.tag requireadmin True/False`"));
                return;
            }

            try
            {
                if (config == null)
                {
                    await _floofDb.TagConfigs.AddAsync(new TagConfig
                    {
                        ServerId = Context.Guild.Id,
                        TagUpdateRequiresAdmin = parsedRequireAdmin,
                    });
                }
                else
                {
                    config.TagUpdateRequiresAdmin = parsedRequireAdmin;
                }
                await _floofDb.SaveChangesAsync();
                string message = "Adding/removing tags now " +
                    (parsedRequireAdmin ? "requires" : "does not require") +
                    " admin permission.";
                await Context.Channel.SendMessageAsync(message);
            }
            catch (DbUpdateException e)
            {
                string message = "Error when configuring permissions for adding/removing tags.";
                await Context.Channel.SendMessageAsync(message);
                Log.Error(message + Environment.NewLine + e);
            }
        }

        private bool GetTagUpdateRequiresAdmin(ulong serverId)
        {
            // assume if the record doesn't exist in the DB that we do not need admin permission
            TagConfig config = _floofDb.TagConfigs.AsQueryable()
                .FirstOrDefault(config => config.ServerId == serverId);
            return config == null ? false : config.TagUpdateRequiresAdmin;
        }

        private bool UserHasTagUpdatePermissions(IGuildUser user)
        {
            bool requireAdminPermissions = GetTagUpdateRequiresAdmin(Context.Guild.Id);
            return !requireAdminPermissions || user.GuildPermissions.Administrator;
        }

        [Command("add")]
        [Summary("Adds a tag to the server")]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task Add(
            [Summary("Tag name")] string tagName = null,
            [Summary("Tag content")][Remainder] string content = null)
        {
            IGuildUser user = (IGuildUser)Context.Message.Author;
            if (!UserHasTagUpdatePermissions(user))
            {
                await Context.Channel.SendMessageAsync("You do not have the permission to add tags.");
                return;
            }

            if (!string.IsNullOrEmpty(tagName) && !string.IsNullOrEmpty(content))
            {
                Regex rgx = new Regex("[^a-zA-Z0-9-]");
                string processedTagName = rgx.Replace(tagName, "").ToLower();
                if (string.IsNullOrEmpty(processedTagName))
                {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Invalid Tag name. " +
                        "Tag must contain characters within [A-Za-z0-9-]."));
                    return;
                }

                bool tagExists = _floofDb.Tags.AsQueryable()
                    .Any(tag => tag.TagName == processedTagName && tag.ServerId == Context.Guild.Id);
                if (tagExists)
                {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Tag `{processedTagName}` already exists"));
                    return;
                }

                try
                {
                    _floofDb.Add(new Tag
                    {
                        TagName = processedTagName,
                        ServerId = Context.Guild.Id,
                        UserId = Context.User.Id,
                        TagContent = content
                    });
                    _floofDb.SaveChanges();
                    await SendEmbed(CreateDescriptionEmbed($"💾 Added Tag `{processedTagName}`"));
                }
                catch (DbUpdateException e)
                {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Unable to add Tag `{processedTagName}`"));
                    Log.Error(e.ToString());
                }
            }
            else
            {
                await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag add [name] [content]`"));
            }
        }

        [Command("list")]
        [Summary("Lists all tags on the server, optionally filtering by keywords")]
        public async Task ListTags([Summary("Keywords to use")][Remainder] string keywords = null)
        {
            List<Tag> tags = _floofDb.Tags.AsQueryable()
                .Where(x => x.ServerId == Context.Guild.Id)
                .OrderBy(x => x.TagName)
                .ToList();

            if (tags.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No tags have been added yet");
                return;
            }

            // filter tags by keywords if applicable
            if (!string.IsNullOrEmpty(keywords))
            {
                keywords = keywords.ToLower();
                tags = tags.Where(tag => tag.TagName.Split(":")[0].ToLower().Contains(keywords)).ToList();
            }

            if (tags.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No tags found with the given keyword(s)");
                return;
            }

            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();
            int numPages = (int)Math.Ceiling((double)tags.Count / TAGS_PER_PAGE);
            int tagIndex;
            string actualName;
            for (int i = 0; i < numPages; i++)
            {
                string text = "```\n";
                for (int j = 0; j < TAGS_PER_PAGE; j++)
                {
                    tagIndex = i * TAGS_PER_PAGE + j;
                    if (tagIndex < tags.Count)
                    {
                        actualName = tags[tagIndex].TagName.Split(":")[0];
                        text += $"{tagIndex + 1}. {actualName}\n";
                    }
                }
                text += "\n```";
                pages.Add(new PaginatedMessage.Page
                {
                    Description = text
                });
            };

            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = EMBED_COLOR,
                Content = Context.User.Mention,
                FooterOverride = null,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            });
        }

        [Command("remove")]
        [Summary("Removes a tag from the server")]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task Remove([Summary("Tag name")] string tag = null)
        {
            IGuildUser user = (IGuildUser)Context.Message.Author;
            if (!UserHasTagUpdatePermissions(user))
            {
                await Context.Channel.SendMessageAsync("You do not have the permission to remove tags.");
                return;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                string tagName = tag.ToLower();
                Tag tagToRemove = _floofDb.Tags.FirstOrDefault(x => x.TagName == tagName);
                if (tagToRemove != null)
                {
                    try
                    {
                        _floofDb.Remove(tagToRemove);
                        await _floofDb.SaveChangesAsync();
                        await SendEmbed(CreateDescriptionEmbed($"💾 Tag: `{tag}` Removed"));
                    }
                    catch (DbUpdateException)
                    {
                        await SendEmbed(CreateDescriptionEmbed($"💾 Unable to remove Tag: `{tag}`"));
                    }
                }
                else
                {
                    await SendEmbed(CreateDescriptionEmbed($"💾 Could not find Tag: `{tag}`"));
                }
            }
            else
            {
                await SendEmbed(CreateDescriptionEmbed($"💾 Usage: `tag remove [name]`"));
            }
        }

        [Command("")]
        [Summary("Displays a tag")]
        [RequireUserPermission(GuildPermission.AttachFiles)]
        public async Task GetTag([Summary("Tag name")] string tagName = "")
        {
            if (!string.IsNullOrEmpty(tagName))
            {
                tagName = tagName.ToLower();
                Tag selectedTag = _floofDb.Tags.AsQueryable().FirstOrDefault(x => x.TagName == tagName);

                if (selectedTag != null)
                {
                    string mentionlessTagContent = selectedTag.TagContent.Replace("@", "[at]");

                    bool isImage = false;
                    if (Uri.IsWellFormedUriString(mentionlessTagContent, UriKind.RelativeOrAbsolute))
                    {
                        string ext = mentionlessTagContent.Split('.').Last().ToLower();
                        isImage = SUPPORTED_IMAGE_EXTENSIONS.Contains(ext);
                    }

                    // tag found, so post it
                    if (isImage)
                    {
                        EmbedBuilder builder = new EmbedBuilder()
                        {
                            Title = "💾  " + tagName.ToLower(),
                            Color = EMBED_COLOR
                        };
                        builder.WithImageUrl(mentionlessTagContent);
                        await SendEmbed(builder.Build());
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync(mentionlessTagContent);
                    }
                }
                else
                {
                    // tag not found
                    await SendEmbed(CreateDescriptionEmbed($"💾 Could not find Tag: `{tagName}`"));
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

        private async Task SendEmbed(Embed embed)
        {
            await Context.Channel.SendMessageAsync("", false, embed);
        }
    }
}