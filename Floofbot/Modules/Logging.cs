using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;

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
            private FloofDataContext _floofDB;

            public LoggerCommands(FloofDataContext floofDB)
            {
                _floofDB = floofDB;
            }

            private void CheckServer(ulong server)
            {
                // checks if server exists in database and adds if not
                var serverConfig = _floofDB.LogConfigs.Find(server);
                if (serverConfig == null)
                {
                    _floofDB.Add(new LogConfig {
                                                ServerId = server,
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

            private async Task SetChannel(string tableName, Discord.IChannel channel, Discord.IGuild guild)
            {
                CheckServer(guild.Id);

                // set channel
                _floofDB.Database.ExecuteSqlRaw($"UPDATE LogConfigs SET {tableName} = {channel.Id} WHERE ServerID = {guild.Id}");
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Channel updated! Set " + tableName + " to <#" + channel.Id + ">");
            }


            [Summary("Applies a channel type onto a channel")]
            [Command("setchannel")]
            public async Task Channel(
                [Summary("channel type")] string messageType,
                [Summary("channel")] Discord.IChannel channel)
            {
                var MessageTypes = new List<string> {
                            "MessageUpdatedChannel",
                            "MessageDeletedChannel",
                            "UserBannedChannel",
                            "UserUnbannedChannel",
                            "UserJoinedChannel",
                            "UserLeftChannel",
                            "MemberUpdatesChannel",
                            "UserKickedChannel",
                            "UserMutedChannel",
                            "UserUnmutedChannel"
                            };
                if (MessageTypes.Contains(messageType))
                {
                    await SetChannel(messageType, channel, Context.Guild);
                }
                else
                {
                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Description = $"" +
                        $"💾 Accepted Channels: ``MessageUpdatedChannel, MessageDeletedChannel, " +
                        $"UserBannedChannel, UserUnbannedChannel, UserJoinedChannel, UserLeftChannel, MemberUpdatesChannel, " +
                        $"UserKickedChannel, UserMutedChannel, UserUnmutedChannel]``",
                        Color = Color.Magenta
                    };
                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
            }

            [Summary("Enable/disable the logger")]
            [Command("toggle")]
            public async Task Toggle()
            {

                CheckServer(Context.Guild.Id);

                // try toggling
                try
                {
                    // check the status of logger
                    var ServerConfig = _floofDB.LogConfigs.Find(Context.Guild.Id);

                    bool bEnabled = ServerConfig.IsOn;
                    if (!bEnabled)
                    {
                        ServerConfig.IsOn = true;
                        await Context.Channel.SendMessageAsync("Logger Enabled!");
                    }
                    else if (bEnabled)
                    {
                        ServerConfig.IsOn = false;
                        await Context.Channel.SendMessageAsync("Logger Disabled!");
                    }
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


