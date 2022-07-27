using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Linq;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using System.Collections.Generic;

namespace Floofbot.Modules
{
    [Summary("Settings that control the automatic word filtering in a server")]
    [Name("Filter")]
    [Group("filter")]
    [Alias("f")]
    [RequireContext(ContextType.Guild)]
    [RequireUserPermission(GuildPermission.BanMembers)]
    public class Filter : InteractiveBase
    {
        private static readonly Color EMBED_COLOR = Color.Magenta;
        private static readonly int WORDS_PER_PAGE = 50;
        private FloofDataContext _floofDb;
        
        public Filter(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }
        
        private async Task CheckServerEntryExists(ulong server)
        {
            // Checks if server exists in database and adds if is not
            var serverConfig = _floofDb.FilterConfigs.Find(server);
            
            if (serverConfig == null)
            {
                _floofDb.Add(new FilterConfig
                {
                    ServerId = server,
                    IsOn = false
                });
                
                await _floofDb.SaveChangesAsync();
            }
        }
        
        private bool CheckWordEntryExists(string word, SocketGuild guild)
        {
            // Checks if a word exists in the filter db
            var wordEntry = _floofDb.FilteredWords.AsQueryable().Where(w => w.ServerId == guild.Id).Any(w => w.Word == word);
            
            return wordEntry;
        }

        [Command("toggle")]
        [Summary("Toggles the server/channel-level word filter")]
        public async Task Toggle([Summary("Either 'channel' or 'server'")] string toggleType = null)
        {
            if (toggleType == "server")
            {
                // Try toggling
                try
                {
                    await CheckServerEntryExists(Context.Guild.Id);
                    // Check the status of server filtering
                    var serverConfig = _floofDb.FilterConfigs.Find(Context.Guild.Id);
                    
                    serverConfig.IsOn = !serverConfig.IsOn;
                    
                    await _floofDb.SaveChangesAsync();
                    
                    await Context.Channel.SendMessageAsync("Server Filtering " + (serverConfig.IsOn ? "Enabled!" : "Disabled!"));
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    
                    Serilog.Log.Error("Error when trying to toggle the server filtering: " + ex);
                }
            }
            else if (toggleType == "channel")
            {
                // Try toggling
                try
                {
                    await CheckServerEntryExists(Context.Guild.Id);
                    
                    // Check the status of logger
                    var channelData = await _floofDb.FilterChannelWhitelists.FindAsync(Context.Channel.Id);
                    bool channelInDatabase;

                    if (channelData == null)
                    {
                        _floofDb.Add(new FilterChannelWhitelist
                        {
                            ChannelId = Context.Channel.Id,
                            ServerId = Context.Guild.Id
                        });
                        
                        await _floofDb.SaveChangesAsync();
                        
                        channelInDatabase = true;
                    }
                    else
                    {
                        _floofDb.FilterChannelWhitelists.Remove(channelData);
                        
                        await _floofDb.SaveChangesAsync();
                        
                        channelInDatabase = false;
                    }
                    
                    await Context.Channel.SendMessageAsync("Filtering For This Channel Is " + (!channelInDatabase ? "Enabled!" : "Disabled!"));
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    
                    Serilog.Log.Error("Error when trying to toggle the channel filtering: " + ex);
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `filter toggle channel/server`", Color = EMBED_COLOR }.Build());
            }
        }

        [Summary("Adds a new filtered word")]
        [Command("add")]
        public async Task AddFilteredWord([Summary("filtered word")][Remainder] string word)
        {
            await CheckServerEntryExists(Context.Guild.Id);
            
            var newWord = word.ToLower();
            var wordAlreadyExists = CheckWordEntryExists(newWord, Context.Guild);

            if (wordAlreadyExists)
            {
                await Context.Channel.SendMessageAsync($"{word} is already being filtered!");
                return;
            }

            try
            {
                _floofDb.Add(new FilteredWord
                {
                    Word = newWord,
                    ServerId = Context.Guild.Id
                });
                
                await _floofDb.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync($"{word} is now being filtered!");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                
                Serilog.Log.Error("Error when trying to add a filtered word: " + ex);
            }
        }
        
        [Summary("Removed an existing filtered word")]
        [Command("remove")]
        public async Task RemoveFilteredWord([Summary("filtered word")][Remainder] string word)
        {
            await CheckServerEntryExists(Context.Guild.Id);
            
            var oldWord = word.ToLower();
            var wordAlreadyExists = CheckWordEntryExists(oldWord, Context.Guild);

            if (!wordAlreadyExists)
            {
                await Context.Channel.SendMessageAsync($"{word} isn't being filtered!");
                return;
            }

            try
            {
                var wordEntry = _floofDb.FilteredWords.AsQueryable().Where(w => w.ServerId == Context.Guild.Id).First(w => w.Word == oldWord);
                
                _floofDb.Remove(wordEntry);
                
                await _floofDb.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync($"{word} is no longer being filtered!");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                
                Serilog.Log.Error("Error when trying to toggle the channel filtering: " + ex);
            }
        }
        
        [Summary("Lists all filtered words")]
        [Command("list")]
        public async Task ListFilteredWords()
        {
            var filteredWords = _floofDb.FilteredWords.AsQueryable()
                .Where(x => x.ServerId == Context.Guild.Id)
                .OrderBy(x => x.Id)
                .ToList();

            if (filteredWords.Count == 0)
            {
                await Context.Channel.SendMessageAsync("No words have been filtered yet");
                
                return;
            }

            var pages = new List<PaginatedMessage.Page>();
            var numPages = (int)Math.Ceiling((double)filteredWords.Count / WORDS_PER_PAGE);
            
            for (int i = 0; i < numPages; i++)
            {
                var text = "```\n";
                
                for (int j = 0; j < WORDS_PER_PAGE; j++)
                {
                    var index = i * WORDS_PER_PAGE + j;
                    
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
            }

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
