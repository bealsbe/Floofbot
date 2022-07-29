using Discord;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    public class Logging
    {
        [Summary("Logging commands")]
        [Name("Logger Configuration Commands")]
        [Group("logger")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public class LoggerCommands : ModuleBase<SocketCommandContext>
        {
            private static readonly List<string> CHANNEL_TYPES = new List<string> {
                "MessageUpdatedChannel",
                "MessageDeletedChannel",
                "UserBannedChannel",
                "UserUnbannedChannel",
                "UserJoinedChannel",
                "UserLeftChannel",
                "MemberUpdatesChannel",
                "UserKickedChannel",
                "UserMutedChannel",
                "UserUnmutedChannel",
            };
            
            private FloofDataContext _floofDB;

            public LoggerCommands(FloofDataContext floofDB)
            {
                _floofDB = floofDB;
            }

            private void AddServerIfNotExists(ulong serverId)
            {
                // Checks if server exists in database and adds if not
                var serverConfig = _floofDB.LogConfigs.Find(serverId);

                if (serverConfig != null) return;
                
                _floofDB.Add(new LogConfig
                {
                    ServerId = serverId,
                    MessageUpdatedChannel = 0,
                    MessageDeletedChannel = 0,
                    UserBannedChannel = 0,
                    UserUnbannedChannel = 0,
                    UserJoinedChannel = 0,
                    UserLeftChannel = 0,
                    MemberUpdatesChannel = 0,
                    UserKickedChannel = 0,
                    UserMutedChannel = 0,
                    UserUnmutedChannel = 0,
                    IsOn = false
                });
                
                _floofDB.SaveChanges();
            }
            
            private async Task AddServerIfNotExistsAsync(ulong serverId)
            {
                // Checks if server exists in database and adds if not
                var serverConfig = _floofDB.LogConfigs.Find(serverId);

                if (serverConfig != null) return;
                
                _floofDB.Add(new LogConfig
                {
                    ServerId = serverId,
                    MessageUpdatedChannel = 0,
                    MessageDeletedChannel = 0,
                    UserBannedChannel = 0,
                    UserUnbannedChannel = 0,
                    UserJoinedChannel = 0,
                    UserLeftChannel = 0,
                    MemberUpdatesChannel = 0,
                    UserKickedChannel = 0,
                    UserMutedChannel = 0,
                    UserUnmutedChannel = 0,
                    IsOn = false
                });
                
                await _floofDB.SaveChangesAsync();
            }

            private bool TryLinkChannelType(string channelType, Discord.IChannel channel, Discord.IGuild guild)
            {
                var serverId = guild.Id;
                var channelId = channel?.Id ?? 0;
                
                AddServerIfNotExists(serverId);
                
                try
                {
                    var updateCommand = $"UPDATE LogConfigs SET {channelType} = {channelId} WHERE ServerID = {serverId}";
                    
                    _floofDB.Database.ExecuteSqlRaw(updateCommand);
                    _floofDB.SaveChanges();
                    
                    return true;
                }
                catch (Exception e)
                {
                    var errorMsg = $"Error: Unable to link {channelType} to <#{channelId}>";
                    
                    Log.Error(errorMsg + Environment.NewLine + e);
                    
                    return false;
                }
            }

            [Summary("Links or re-links a channel type onto a single channel in a server")]
            [Command("setchannel")]
            public async Task SetChannelType(
                [Summary("channel type")] string channelType = null,
                [Summary("channel (current channel if unspecified)")] Discord.IChannel channel = null)
            {
                // If channel is null we assign the Context.Channel
                channel ??= Context.Channel;

                if (CHANNEL_TYPES.Contains(channelType))
                {
                    if (TryLinkChannelType(channelType, channel, Context.Guild))
                    {
                        await Context.Channel.SendMessageAsync($"Channel updated! Set {channelType} to <#{channel.Id}>");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"Unable to set {channelType} to <#{channel.Id}>");
                    }
                }
                else
                {
                    var builder = new EmbedBuilder
                    {
                        Description = $"💾 Accepted channel types: ```{string.Join(", ", CHANNEL_TYPES)}```",
                        Color = Color.Magenta
                    };
                    
                    await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
                }
            }

            [Summary("Unlinks a channel type from all channels within the server")]
            [Command("unsetchannel")]
            public async Task UnsetChannelType([Summary("channel type")] string channelType = null)
            {
                if (CHANNEL_TYPES.Contains(channelType))
                {
                    if (TryLinkChannelType(channelType, null, Context.Guild))
                    {
                        await Context.Channel.SendMessageAsync($"Channel updated! Unset {channelType}");
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync($"Unable to unset {channelType}");
                    }
                }
                else
                {
                    var builder = new EmbedBuilder
                    {
                        Description = $"💾 Accepted channel types: ```{string.Join(", ", CHANNEL_TYPES)}```",
                        Color = Color.Magenta
                    };
                    
                    await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
                }
            }

            [Summary("Enable/disable the logger")]
            [Command("toggle")]
            public async Task Toggle()
            {
                await AddServerIfNotExistsAsync(Context.Guild.Id);
                
                try
                {
                    var serverConfig = await _floofDB.LogConfigs.FindAsync(Context.Guild.Id);
                    serverConfig.IsOn = !serverConfig.IsOn;
                    
                    var statusString = serverConfig.IsOn ? "Enabled" : "Disabled";
                    
                    await Context.Channel.SendMessageAsync($"Logger {statusString}!");
                    
                    await _floofDB.SaveChangesAsync();
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + e.Message);
                    
                    Log.Error("Error when trying to toggle the event logger: " + e);
                }
            }
        }
    }
}
