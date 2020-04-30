using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;
using System.Data.Common;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Linq;

namespace Floofbot.Modules
{
    public class Logging
    {
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

            protected void CheckServer(ulong server)
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

            protected async Task SetChannel(string tableName, Discord.IChannel channel, Discord.IGuild guild)
            {
                CheckServer(guild.Id);

                // set channel
                _floofDB.Database.ExecuteSqlRaw($"UPDATE LogConfigs SET {tableName} = {channel.Id} WHERE ServerID = {guild.Id}");
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Channel updated! Set " + tableName + " to <#" + channel.Id + ">");
            }


            [Command("setchannel")] // update into a group
            public async Task Channel(string messageType, Discord.IChannel channel)
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

        // events handling
        public class EventHandlingService{

            FloofDataContext _floofDb;
            public EventHandlingService(FloofDataContext floofDb)
            {
                _floofDb = floofDb;
            }
            public async Task<ITextChannel> GetChannel(Discord.IGuild guild, string eventName = null)
            {
                if (eventName == null)
                    return null;

                var serverConfig = _floofDb.LogConfigs.Find(guild.Id);
                System.Reflection.PropertyInfo propertyInfo = serverConfig.GetType().GetProperty(eventName);
                ulong logChannel = (ulong)(propertyInfo.GetValue(serverConfig, null));
                var textChannel = await guild.GetTextChannelAsync(logChannel);
                return textChannel;
            }
            public bool IsToggled(IGuild guild)
            {
                // check if the logger is toggled on in this server
                // check the status of logger
                var ServerConfig = _floofDb.LogConfigs.Find(guild.Id);
                if (ServerConfig == null) // no entry in DB for server - not configured
                    return false;

                return ServerConfig.IsOn;
            }

            public async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel chan)
            {
                try
                {
                    // deal with empty message
                    var messageBefore = (before.HasValue ? before.Value : null) as IUserMessage;
                    if (messageBefore == null)
                        return;

                    var channel = chan as ITextChannel; // channel null, dm message?
                    if (channel == null)
                        return;

                    if (messageBefore.Content == after.Content) // no change
                        return;

                    if ((IsToggled(channel.Guild)) == false) // not toggled on
                        return;

                    Discord.ITextChannel logChannel = await GetChannel(channel.Guild, "MessageEditedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"⚠️ Message Edited | {after.Author.Username}")
                     .WithColor(Color.DarkGrey)
                     .WithDescription($"{after.Author.Mention} ({after.Author.Id}) has edited their message in {channel.Mention}!")
                     .AddField("Before", messageBefore.Content)
                     .AddField("After", after.Content)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(after.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(after.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the message updated event handler: " + ex);
                    return;
                }

            }
            public async Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel chan)
            {
                try
                {

                    // deal with empty message
                    var message = (before.HasValue ? before.Value : null) as IUserMessage;
                    if (message == null)
                        return;

                    var channel = chan as ITextChannel; // channel null, dm message?
                    if (channel == null)
                        return;

                    if ((IsToggled(channel.Guild)) == false) // not toggled on
                        return;

                    Discord.ITextChannel logChannel = await GetChannel(channel.Guild, "MessageDeletedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"⚠️ Message Deleted | {message.Author.Username}")
                     .WithColor(Color.Gold)
                     .WithDescription($"{message.Author.Mention} ({message.Author.Id}) has had their message deleted in {channel.Mention}!")
                     .AddField("Content", message.Content)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(message.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(message.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the message deleted event handler: " + ex);
                    return;
                }
            }
            public async Task MessageDeletedByBot(SocketMessage before, ITextChannel channel, string reason = "N/A")
            {
                try
                {
                    // deal with empty message
                    if (before.Content == null)
                        return;

                    if (channel == null)
                        return;

                    if ((IsToggled(channel.Guild)) == false) // not toggled on
                        return;

                    Discord.ITextChannel logChannel = await GetChannel(channel.Guild, "MessageDeletedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"⚠️ Message Deleted By Bot | {before.Author.Username}")
                     .WithColor(Color.Gold)
                     .WithDescription($"{before.Author.Mention} ({before.Author.Id}) has had their message deleted in {channel.Mention}!")
                     .AddField("Content", before.Content)
                     .AddField("Reason", reason)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(before.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(before.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the message deleted by bot event handler: " + ex);
                    return;
                }
            }
            public async Task UserBanned(IUser user, IGuild guild)
            {
                try
                {

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserBannedChannel");
                    if (channel == null)
                        return;

                    var banReason = guild.GetBanAsync(user.Id).Result.Reason;

                    var embed = new EmbedBuilder()
                     .WithTitle($"🔨 User Banned | {user.Username}")
                     .WithColor(Color.Red)
                     .WithDescription($"{user.Mention} | ``{user.Id}``")
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (banReason == null)
                        embed.AddField("Reason", "No Reason Provided");
                    else
                        embed.AddField("Reason", banReason);


                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user banned event handler: " + ex);
                    return;
                }

            }
            public async Task UserUnbanned(IUser user, IGuild guild)
            {
                try
                {

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserUnbannedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                    .WithTitle($"♻️ User Unbanned | {user.Username}")
                    .WithColor(Color.Gold)
                    .WithDescription($"{user.Mention} | ``{user.Id}``")
                    .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user unbanned event handler: " + ex);
                    return;
                }

            }
            public async Task UserJoined(IGuildUser user)
            {
                try
                {
                    if ((IsToggled(user.Guild)) == false)
                        return;
                    Discord.ITextChannel channel = await GetChannel(user.Guild, "UserJoinedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                    .WithTitle($"✅ User Joined | {user.Username}")
                    .WithColor(Color.Green)
                    .WithDescription($"{user.Mention} | ``{user.Id}``")
                    .AddField("Joined Server", user.JoinedAt)
                    .AddField("Joined Discord", user.CreatedAt)
                    .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());
                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user joined event handler: " + ex);
                    return;
                }
            }
            public async Task UserLeft(IGuildUser user)
            {
                try
                {
                    if ((IsToggled(user.Guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(user.Guild, "UserLeftChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                    .WithTitle($"❌ User Left | {user.Username}")
                    .WithColor(Color.Red)
                    .WithDescription($"{user.Mention} | ``{user.Id}``")
                    .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user left event handler: " + ex);
                    return;
                }

            }
            public async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
            {
                try
                {
                    if (before == null || after == null) // empty user params
                        return;
                    var user = after as SocketGuildUser;

                    if ((IsToggled(user.Guild) == false)) // turned off
                        return;

                    Discord.ITextChannel channel = await GetChannel(user.Guild, "MemberUpdatesChannel");
                    if (channel == null) // no log channel set
                        return;

                    var embed = new EmbedBuilder();

                    if (before.Username != after.Username)
                    {
                        embed.WithTitle($"👥 Username Changed | {user.Username}")
                            .WithColor(Color.Purple)
                            .WithDescription($"{user.Mention} | ``{user.Id}``")
                            .AddField("Old Username", user.Username)
                            .AddField("New Name", user.Username)
                            .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                        if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(user.GetAvatarUrl());

                    }
                    else if (before.Nickname != after.Nickname)
                    {
                        embed.WithTitle($"👥 Nickname Changed | {user.Username}")
                            .WithColor(Color.Purple)
                            .WithDescription($"{user.Mention} | ``{user.Id}``")
                            .AddField("Old Nickname", before.Nickname)
                            .AddField("New Nickname", user.Nickname)
                            .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                        if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(user.GetAvatarUrl());

                    }
                    else if (before.AvatarId != after.AvatarId)
                    {
                        embed.WithTitle($"🖼️ Avatar Changed | {user.Username}")
                        .WithColor(Color.Purple)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                        if (Uri.IsWellFormedUriString(before.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(before.GetAvatarUrl());
                        if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithImageUrl(after.GetAvatarUrl());
                    }
                    else if (before.Roles.Count != after.Roles.Count)
                    {
                        List<SocketRole> beforeRoles = new List<SocketRole>(before.Roles);
                        List<SocketRole> afterRoles = new List<SocketRole>(after.Roles);
                        List<SocketRole> roleDifference = new List<SocketRole>();

                        if (before.Roles.Count > after.Roles.Count) // roles removed
                        {
                            roleDifference = beforeRoles.Except(afterRoles).ToList();
                            embed.WithTitle($"❗ Roles Removed | {user.Username}")
                            .WithColor(Color.Orange)
                            .WithDescription($"{user.Mention} | ``{user.Id}``")
                            .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                            foreach (SocketRole role in roleDifference)
                            {
                                embed.AddField("Role Removed", role);
                            }

                            if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                                embed.WithThumbnailUrl(user.GetAvatarUrl());
                        }
                        else if (before.Roles.Count < after.Roles.Count) // roles added
                        {
                            roleDifference = afterRoles.Except(beforeRoles).ToList();
                            embed.WithTitle($"❗ Roles Added | {user.Username}")
                            .WithColor(Color.Orange)
                            .WithDescription($"{user.Mention} | ``{user.Id}``")
                            .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                            foreach (SocketRole role in roleDifference)
                            {
                                embed.AddField("Role Added", role);
                            }
                            if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                                embed.WithThumbnailUrl(user.GetAvatarUrl());
                        }
                    }
                    else
                    {
                        return;
                    }
                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the guild member updated event handler: " + ex);
                    return;
                }

            }
            public async Task UserKicked(IUser user, IUser kicker, IGuild guild)
            {
                try
                {

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserKickedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"👢 User Kicked | {user.Username}")
                     .WithColor(Color.Red)
                     .WithDescription($"{user.Mention} | ``{user.Id}``")
                     .AddField("Kicked By", kicker.Mention)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user kicked event handler: " + ex);
                    return;
                }
            }
            public async Task UserMuted(IUser user, IUser muter, IGuild guild)
            {
                try
                {
                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserMutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"🔇 User Muted | {user.Username}")
                     .WithColor(Color.Teal)
                     .WithDescription($"{user.Mention} | ``{user.Id}``")
                     .AddField("Muted By", muter.Mention)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user muted event handler: " + ex);
                    return;
                }
            }
            public async Task UserUnmuted(IUser user, IUser unmuter, IGuild guild)
            {
                try
                {

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserUnmutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                     .WithTitle($"🔊 User Unmuted | {user.Username}")
                     .WithColor(Color.Teal)
                     .WithDescription($"{user.Mention} | ``{user.Id}``")
                     .AddField("Unmuted By", unmuter.Mention)
                     .WithFooter(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the user unmuted event handler: " + ex);
                    return;
                }
            }



        }
    }
}


