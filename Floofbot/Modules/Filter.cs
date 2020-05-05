using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Linq;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using System.Collections.Generic;

namespace Floofbot.Modules
{
    [Group("filter")]
    [Alias("f")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    [Summary("Settings that control the automatic word filtering in a server")]
    public class Filter : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;
        private static readonly int WORDS_PER_PAGE = 50;
        private FloofDataContext _floofDb;
        public Filter(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }
        private void CheckServerEntryExists(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDb.FilterConfigs.Find(server);
            if (serverConfig == null)
            {
                _floofDb.Add(new FilterConfig
                {
                    ServerId = server,
                    IsOn = false
                });
                _floofDb.SaveChanges();
            }
        }
        private bool CheckWordEntryExists(string word, SocketGuild guild)
        {
            // checks if a word exists in the filter db
            bool wordEntry = _floofDb.FilteredWords.AsQueryable().Where(w => w.ServerId == guild.Id).Where(w => w.Word == word).Any();
            return wordEntry;
        }
        [Command("toggle")]
        [Summary("Toggles the server/channel-level word filter")]
        public async Task Toggle()
        {
            await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `filter toggle channel/server`", Color = EMBED_COLOR }.Build());
        }

        [Command("toggle")]
        [Summary("Toggles the word filter")]
        public async Task Toggle([Summary("Either 'channel' or 'server'")]string toggleType)
        {
            if (toggleType == "server")
            {
                // try toggling
                try
                {
                    CheckServerEntryExists(Context.Guild.Id);
                    // check the status of server filtering
                    var ServerConfig = _floofDb.FilterConfigs.Find(Context.Guild.Id);
                    ServerConfig.IsOn = !ServerConfig.IsOn;
                    _floofDb.SaveChanges();
                    await Context.Channel.SendMessageAsync("Server Filtering " + (ServerConfig.IsOn ? "Enabled!" : "Disabled!"));
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    Serilog.Log.Error("Error when trying to toggle the server filtering: " + ex);
                    return;
                }
            }
            else if (toggleType == "channel")
            {
                // try toggling
                try
                {
                    CheckServerEntryExists(Context.Guild.Id);
                    // check the status of logger
                    var channelData = _floofDb.FilterChannelWhitelists.Find(Context.Channel.Id);
                    bool channelInDatabase = false;

                    if (channelData == null)
                    {
                        _floofDb.Add(new FilterChannelWhitelist
                        {
                            ChannelId = Context.Channel.Id,
                            ServerId = Context.Guild.Id
                        });
                        _floofDb.SaveChanges();
                        channelInDatabase = true;
                    }
                    else
                    {
                        _floofDb.FilterChannelWhitelists.Remove(channelData);
                        _floofDb.SaveChanges();
                        channelInDatabase = false;
                    }
                    await Context.Channel.SendMessageAsync("Filtering For This Channel Is " + (!channelInDatabase ? "Enabled!" : "Disabled!"));
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    Serilog.Log.Error("Error when trying to toggle the channel filtering: " + ex);
                    return;
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `filter toggle channel/server`", Color = EMBED_COLOR }.Build());
            }
            return;
        }
        [Summary("Adds a new filtered word")]
        [Command("add")]
        public async Task AddFilteredWord([Summary("filtered word")]string word)
        {
            CheckServerEntryExists(Context.Guild.Id);
            string newWord = word.ToLower();
            bool wordAlreadyExists = CheckWordEntryExists(newWord, Context.Guild);

            if (wordAlreadyExists)
            {
                await Context.Channel.SendMessageAsync($"{word} is already being filtered!");
                return;
            }
            else
            {
                try
                {
                    _floofDb.Add(new FilteredWord
                    {
                        Word = newWord,
                        ServerId = Context.Guild.Id
                    });
                    _floofDb.SaveChanges();
                    await Context.Channel.SendMessageAsync($"{word} is now being filtered!");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    Serilog.Log.Error("Error when trying to add a filtered word: " + ex);
                }
            }
        }
        [Summary("Removed an existing filtered word")]
        [Command("remove")]
        public async Task RemoveFilteredWord([Summary("filtered word")]string word)
        {
            CheckServerEntryExists(Context.Guild.Id);
            string oldWord = word.ToLower();
            bool wordAlreadyExists = CheckWordEntryExists(oldWord, Context.Guild);

            if (!wordAlreadyExists)
            {
                await Context.Channel.SendMessageAsync($"{word} isn't being filtered!");
                return;
            }
            else
            {
                try
                {
                    FilteredWord wordEntry = _floofDb.FilteredWords.AsQueryable().Where(w => w.ServerId == Context.Guild.Id).Where(w => w.Word == oldWord).First();
                    _floofDb.Remove(wordEntry);
                    _floofDb.SaveChanges();
                    await Context.Channel.SendMessageAsync($"{word} is no longer being filtered!");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    Serilog.Log.Error("Error when trying to toggle the channel filtering: " + ex);
                }
            }
        }
        [Summary("Lists all filtered words")]
        [Command("list")]
        public async Task ListFilteredWords()
        {
            List<FilteredWord> filteredWords = _floofDb.FilteredWords.AsQueryable()
                .Where(x => x.ServerId == Context.Guild.Id)
                .OrderBy(x => x.Id)
                .ToList();

            if (filteredWords.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No words have been filtered yet");
                return;
            }

            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();
            int numPages = (int)Math.Ceiling((double)filteredWords.Count / WORDS_PER_PAGE);
            int index;
            for (int i = 0; i < numPages; i++)
            {
                string text = "```\n";
                for (int j = 0; j < WORDS_PER_PAGE; j++)
                {
                    index = i * WORDS_PER_PAGE + j;
                    if (index < filteredWords.Count)
                    {
                        text += $"{index + 1}. {filteredWords[index].Word}\n";
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
    }
}
