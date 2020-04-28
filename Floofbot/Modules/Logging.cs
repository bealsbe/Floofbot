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

namespace Floofbot.Modules
{
    public class Logging
    {
        [Group("logger")]
        [RequireUserPermission(GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        public class LoggerCommands : ModuleBase<SocketCommandContext>
        {
            SqliteConnection dbConnection;

            public LoggerCommands()
            {
                dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = "botdata.db"
                }.ToString());
                dbConnection.Open();

                // first check the structure exist before we use the module
                string sqlCreateStructure = @"
                        CREATE TABLE IF NOT EXISTS Logger(
                            'ServerID' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'MessageUpdatedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'MessageDeletedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserBannedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserUnbannedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserJoinedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserLeftChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'MemberUpdatesChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserKickedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserMutedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'UserUnmutedChannel' UNSIGNED BIT INT DEFAULT 0 NOT NULL,
                            'IsOn' INT DEFAULT 0 NOT NULL)
                      ";
                using (SqliteCommand cmdCreateStructure = new SqliteCommand(sqlCreateStructure, dbConnection))
                {
                    cmdCreateStructure.ExecuteScalar();
                }
            }

            protected void CheckServer(ulong ServerId)
            {
                // checks if server exists in database and adds if not
                string sqlInsertServer = @"
                                            INSERT INTO Logger(ServerID,
                                                                MessageUpdatedChannel,
                                                                MessageDeletedChannel,
                                                                UserBannedChannel,
                                                                UserUnbannedChannel,
                                                                UserJoinedChannel,
                                                                UserLeftChannel,
                                                                MemberUpdatesChannel,
                                                                UserKickedChannel,
                                                                UserMutedChannel,
                                                                UserUnmutedChannel,
                                                                IsOn)
                                            SELECT * FROM (SELECT $ServerID,0,0,0,0,0,0,0,0,0,0,0) AS tmp
                                            WHERE NOT EXISTS (
                                                              SELECT * FROM Logger WHERE ServerID = $ServerID
                                                              ) LIMIT 1;
                                          ";
                using (SqliteCommand cmdInsertServer = new SqliteCommand(sqlInsertServer, dbConnection))
                {
                    cmdInsertServer.Parameters.Add(new SqliteParameter("$ServerID", ServerId));
                    cmdInsertServer.ExecuteNonQuery();
                }
            }

            protected async Task SetChannel(string tableName, Discord.IChannel channel, Discord.IGuild guild)
            {
                CheckServer(guild.Id);

                // set channel
                string sqlUpdateChannel = $"UPDATE Logger SET {tableName} = {channel.Id} WHERE ServerID = {guild.Id}";
                using (SqliteCommand cmd = new SqliteCommand(sqlUpdateChannel, dbConnection))
                {
                    cmd.ExecuteNonQuery();
                }
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
                    // some sort of thing telling them what to put
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
                    string sqlCheckStatus = $"SELECT * FROM Logger WHERE ServerID = {Context.Guild.Id} LIMIT 1"; // check state
                    using (SqliteCommand command = new SqliteCommand(sqlCheckStatus, dbConnection))
                    {
                        using (SqliteDataReader result = command.ExecuteReader())
                        {
                            while (result.Read())
                            {
                                long bEnabled = (long)result["IsOn"];
                                if (bEnabled == 0)
                                {
                                    string sqlToggleStatus = $"UPDATE Logger SET IsOn = 1 WHERE ServerID = {Context.Guild.Id}";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                    await Context.Channel.SendMessageAsync("Logger Enabled!");
                                }
                                else if (bEnabled == 1)
                                {
                                    string sqlToggleStatus = $"UPDATE Logger SET IsOn = 0 WHERE ServerID = {Context.Guild.Id}";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                    await Context.Channel.SendMessageAsync("Logger Disabled!");
                                }
                                else // should never happen, but incase it does, reset the value
                                {
                                    await Context.Channel.SendMessageAsync("Unable to toggle logger. Try again");
                                    string sqlToggleStatus = $"UPDATE Logger SET IsOn = 0 WHERE ServerID = {Context.Guild.Id}";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                }
            }

        }

        // events handling
        public class EventHandlingService{

            SqliteConnection dbConnection;
            public EventHandlingService()
            {
                dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = "botdata.db"
                }.ToString());
                dbConnection.Open();
            }
            public async Task<ITextChannel> GetChannel(string tableName, Discord.IGuild guild)
            {
                // gets a channel based on the tablename (the type of logger)
                string sqlGetChannel = $"SELECT {tableName} FROM Logger WHERE ServerID = {guild.Id} LIMIT 1"; // check state
                using (SqliteCommand command = new SqliteCommand(sqlGetChannel, dbConnection))
                {
                    using (SqliteDataReader result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            if (result.HasRows)
                            {
                                ulong channelID = Convert.ToUInt64(result[tableName]);
                                if (channelID == 0)
                                    return null; // they have not set a logger channel
                                // get channel object
                                var textChannel = await guild.GetTextChannelAsync(channelID);
                                return textChannel;
                            }
                            else
                            {
                                // channel entry not there? Incorrect cast? 
                                Log.Error("Tried to log {tableName} but could not find the associated database entry", tableName); return null;
                            }

                        }
                        return null;
                    }
                }
            }
            public bool IsToggled(IGuild guild)
            {
                // check if the logger is toggled on in this server
                // check the status of logger
                string sqlCheckStatus = $"SELECT IsOn FROM Logger WHERE ServerID = {guild.Id} LIMIT 1"; // check state
                using (SqliteCommand command = new SqliteCommand(sqlCheckStatus, dbConnection))
                {
                    using (SqliteDataReader result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            long bEnabled = (long)result["IsOn"];
                            if (bEnabled == 0)
                                return false;
                            else if (bEnabled == 1)
                                return true;
                            else
                                return false;
                        }
                        return false;
                    }
                }
            }

            public async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel chan)
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

                Discord.ITextChannel logChannel = await GetChannel("MessageEditedChannel", channel.Guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Edited | {after.Author.Username}")
                 .WithColor(Color.DarkGrey)
                 .WithDescription($"{after.Author.Mention} ({after.Author.Id}) has edited their message in {channel.Mention}!")
                 .AddField("Before", messageBefore.Content)
                 .AddField("After", after.Content)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(after.Author.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(after.Author.GetAvatarUrl());

                await logChannel.SendMessageAsync("", false, embed.Build());
            }
            public async Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel chan)
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

                Discord.ITextChannel logChannel = await GetChannel("MessageDeletedChannel", channel.Guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Deleted | {message.Author.Username}")
                 .WithColor(Color.Gold)
                 .WithDescription($"{message.Author.Mention} ({message.Author.Id}) has had their message deleted in {channel.Mention}!")
                 .AddField("Content", message.Content)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(message.Author.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(message.Author.GetAvatarUrl());

                await logChannel.SendMessageAsync("", false, embed.Build());
            }
            public async Task MessageDeletedByBot(SocketMessage before, ITextChannel channel, string reason = "N/A")
            {

                // deal with empty message
                if (before.Content == null)
                    return;

                if (channel == null)
                    return;

                if ((IsToggled(channel.Guild)) == false) // not toggled on
                    return;

                Discord.ITextChannel logChannel = await GetChannel("MessageDeletedChannel", channel.Guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Deleted By Bot | {before.Author.Username}")
                 .WithColor(Color.Gold)
                 .WithDescription($"{before.Author.Mention} ({before.Author.Id}) has had their message deleted in {channel.Mention}!")
                 .AddField("Content", before.Content)
                 .AddField("Reason", reason)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(before.Author.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(before.Author.GetAvatarUrl());

                await logChannel.SendMessageAsync("", false, embed.Build());
            }
            public async Task UserBanned(IUser user, IGuild guild)
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserBannedChannel", guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"🔨 User Banned | {user.Username}")
                 .WithColor(Color.Red)
                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                 .WithFooter(DateTime.Now.ToString());

                 if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                     embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());

            }
            public async Task UserBannedByBot(IUser user, IGuild guild, string reason = "N/A")
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserBannedChannel", guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"🔨 User Banned | {user.Username}")
                 .WithColor(Color.Red)
                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                 .AddField("Reason", reason)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());

            }
            public async Task UserUnbanned(IUser user, IGuild guild)
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserUnbannedChannel", guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                .WithTitle($"♻️ User Unbanned | {user.Username}")
                .WithColor(Color.Gold)
                .WithDescription($"{user.Mention} | ``{user.Id}``")
                .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());

            }
            public async Task UserJoined(IGuildUser user)
            {
                try
                {
                    if ((IsToggled(user.Guild)) == false)
                        return;
                    Discord.ITextChannel channel = await GetChannel("UserJoinedChannel", user.Guild);
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                    .WithTitle($"✅ User Joined | {user.Username}")
                    .WithColor(Color.Green)
                    .WithDescription($"{user.Mention} | ``{user.Id}``")
                    .AddField("Joined Server", user.JoinedAt)
                    .AddField("Joined Discord", user.CreatedAt)
                    .WithFooter(DateTime.Now.ToString());

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());
                    await channel.SendMessageAsync("", false, embed.Build());
                }
                catch(Exception ex)
                {
                    Console.Write(ex);
                }
            }
            public async Task UserLeft(IGuildUser user)
            {
                if ((IsToggled(user.Guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserLeftChannel", user.Guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                .WithTitle($"❌ User Left | {user.Username}")
                .WithColor(Color.Red)
                .WithDescription($"{user.Mention} | ``{user.Id}``")
                .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());

            }
            public async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
            {

                if (before == null || after == null) // empty user params
                    return;
                var user = after as SocketGuildUser;

                if ((IsToggled(user.Guild) == false)) // turned off
                    return;

                Discord.ITextChannel channel = await GetChannel("MemberUpdatesChannel", user.Guild);
                if (channel == null) // no log channel set
                    return;

                var embed = new EmbedBuilder();

                if (before.Username != after.Username)
                {
                    embed.WithTitle($"👥 Username Changed | {user.Mention}")
                        .WithColor(Color.Purple)
                        .WithDescription($"<@{before.Id}> | ``{before.Id}``")
                        .AddField("Old Username", user.Username)
                        .AddField("New Name", user.Username)
                        .WithFooter(DateTime.Now.ToString());

                }
                else if (before.Nickname != after.Nickname)
                {
                    embed.WithTitle($"👥 Nickname Changed | {user.Mention}")
                        .WithColor(Color.Purple)
                        .WithDescription($"<@{before.Id}> | ``{before.Id}``")
                        .AddField("Old Nickname", before.Nickname)
                        .AddField("New Nickname", user.Nickname)
                        .WithFooter(DateTime.Now.ToString());

                }
                else if (before.AvatarId != after.AvatarId)
                {
                    embed.WithTitle($"🖼️ Avatar Changed | {user.Mention}")
                    .WithColor(Color.Purple)
                    .WithDescription($"<@{before.Id}> | ``{before.Id}``")
                    .WithFooter(DateTime.Now.ToString());
                    if (Uri.IsWellFormedUriString(before.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(before.GetAvatarUrl());
                    if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithImageUrl(after.GetAvatarUrl());
                }
                else
                {
                    return;
                }
                await channel.SendMessageAsync("", false, embed.Build());


            }
            public async Task UserKicked(IUser user, IUser kicker, IGuild guild)
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserKickedChannel", guild);
                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"👢 User Kicked | {user.Username}")
                 .WithColor(Color.Red)
                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                 .AddField("Kicked By", kicker.Mention)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());
            }
            public async Task UserMuted(IUser user, IUser muter, IGuild guild)
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserMutedChannel", guild);

                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"🔇 User Muted | {user.Username}")
                 .WithColor(Color.Teal)
                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                 .AddField("Muted By", muter.Mention)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());
            }
            public async Task UserUnmuted(IUser user, IUser unmuter, IGuild guild)
            {
                if ((IsToggled(guild)) == false)
                    return;

                Discord.ITextChannel channel = await GetChannel("UserUnmutedChannel", guild);

                if (channel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"🔊 User Unmuted | {user.Username}")
                 .WithColor(Color.Teal)
                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                 .AddField("Unmuted By", unmuter.Mention)
                 .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());

                await channel.SendMessageAsync("", false, embed.Build());
            }



        }
    }
}


