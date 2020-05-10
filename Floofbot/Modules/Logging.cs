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
        [Discord.Commands.Name("Logger")]
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
                // checks if server exists in database and adds if not
                LogConfig serverConfig = _floofDB.LogConfigs.Find(serverId);
                if (serverConfig == null)
                {
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
            }

            private bool TryLinkChannelType(string channelType, Discord.IChannel channel, Discord.IGuild guild)
            {
                ulong serverId = guild.Id;
                ulong channelId = channel == null ? 0 : channel.Id;
                AddServerIfNotExists(serverId);
                try
                {
                    string updateCommand = $"UPDATE LogConfigs SET {channelType} = {channelId} WHERE ServerID = {serverId}";
                    _floofDB.Database.ExecuteSqlRaw(updateCommand);
                    _floofDB.SaveChanges();
                    return true;
                }
                catch (Exception e)
                {
                    string errorMsg = $"Error: Unable to link {channelType} to <#{channelId}>";
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
                if (channel == null)
                {
                    channel = (IChannel)Context.Channel;
                }

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
                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Description = $"💾 Accepted channel types: ```{string.Join(", ", CHANNEL_TYPES)}```",
                        Color = Color.Magenta
                    };
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
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
                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Description = $"💾 Accepted channel types: ```{string.Join(", ", CHANNEL_TYPES)}```",
                        Color = Color.Magenta
                    };
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            [Summary("Enable/disable the logger")]
            [Command("toggle")]
            public async Task Toggle()
            {
                AddServerIfNotExists(Context.Guild.Id);
                try
                {
                    LogConfig serverConfig = _floofDB.LogConfigs.Find(Context.Guild.Id);
                    serverConfig.IsOn = !serverConfig.IsOn;
                    string statusString = serverConfig.IsOn ? "Enabled" : "Disabled";
                    await Context.Channel.SendMessageAsync($"Logger {statusString}!");
                    _floofDB.SaveChanges();
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                    Log.Error("Error when trying to toggle the event logger: " + ex);
                    return;
                }
            }
        }
    }
}
