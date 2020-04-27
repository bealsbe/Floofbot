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
                            'ServerID' INTEGER DEFAULT 0 NOT NULL,
                            'MessageUpdatedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'MessageDeletedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserBannedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserUnbannedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserJoinedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserLeftChannel' INTEGER DEFAULT 0 NOT NULL,
                            'MemberUpdatesChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserKickedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserMutedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'UserUnmutedChannel' INTEGER DEFAULT 0 NOT NULL,
                            'IsOn' INT DEFAULT 0 NOT NULL)
                      ";
                using (SqliteCommand cmdCreateStructure = new SqliteCommand(sqlCreateStructure, dbConnection))
                {
                    cmdCreateStructure.ExecuteScalar();
                }
            }

            public async Task CheckServer(ulong ServerId)
            {
                // checks if server exists in database and adds if not
                string sqlInsertServer = @"
                                            INSERT INTO Logger(ServerID, ChannelID, IsOn)
                                            SELECT * FROM (SELECT $ServerID, 0, 0) AS tmp
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

            [Command("setchannel")]
            public async Task Channel(Discord.IChannel channel)
            {
                await CheckServer(Context.Guild.Id);

                // set channel
                string sqlUpdateChannel = @"UPDATE Logger SET ChannelID = $ChannelID WHERE ServerID = $ServerID";
                using (SqliteCommand cmd = new SqliteCommand(sqlUpdateChannel, dbConnection))
                {
                    cmd.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                    cmd.Parameters.Add(new SqliteParameter("$ChannelID", channel.Id));
                    cmd.ExecuteNonQuery();
                }
                await Context.Channel.SendMessageAsync("Channel updated! I will now send logs to <#" + channel.Id + ">");
            }

            [Command("toggle")]
            public async Task Toggle()
            {

                await CheckServer(Context.Guild.Id);

                // try toggling
                try
                {
                    // check the status of logger
                    string sqlCheckStatus = @"SELECT * FROM Logger WHERE ServerID = $ServerID LIMIT 1"; // check state
                    using (SqliteCommand command = new SqliteCommand(sqlCheckStatus, dbConnection))
                    {
                        command.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                        using (SqliteDataReader result = command.ExecuteReader())
                        {
                            while (result.Read())
                            {
                                long bEnabled = (long)result["IsOn"];
                                if (bEnabled == 0)
                                {
                                    string sqlToggleStatus = @"UPDATE Logger SET IsOn = 1 WHERE ServerID = $ServerID";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                                        cmd.ExecuteNonQuery();
                                    }
                                    await Context.Channel.SendMessageAsync("Logger Enabled!");
                                }
                                else if (bEnabled == 1)
                                {
                                    string sqlToggleStatus = @"UPDATE Logger SET IsOn = 0 WHERE ServerID = $ServerID";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                                        cmd.ExecuteNonQuery();
                                    }
                                    await Context.Channel.SendMessageAsync("Logger Disabled!");
                                }
                                else // should never happen, but incase it does, reset the value
                                {
                                    await Context.Channel.SendMessageAsync("Unable to toggle logger. Try again");
                                    string sqlToggleStatus = @"UPDATE Logger SET IsOn = 0 WHERE ServerID = '$ServerID'";
                                    using (SqliteCommand cmd = new SqliteCommand(sqlToggleStatus, dbConnection))
                                    {
                                        cmd.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
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
            public Discord.IGuildChannel GetChannel(string tableName, Discord.IGuild guild)
            {
                // gets a channel based on the tablename (the type of logger)
                string sqlGetChannel = @"SELECT $TableName FROM Logger WHERE ServerID = $ServerID LIMIT 1"; // check state
                using (SqliteCommand command = new SqliteCommand(sqlGetChannel, dbConnection))
                {
                    command.Parameters.Add(new SqliteParameter("$TableName", tableName));
                    using (SqliteDataReader result = command.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            if (result.HasRows)
                            {
                                ulong channelID = (ulong)result[tableName];
                                if (channelID == 0)
                                    return null; // they have not set a logger channel
                                // get channel object
                                Discord.IGuildChannel textChannel = (Discord.IGuildChannel)guild.GetChannelAsync(Convert.ToUInt64(channelID));
                                return textChannel;
                            }
                            else
                            {
                                // channel entry not there? Incorrect cast? 
                                Console.Write($"Tried to log {tableName} but could not find the associated database entry!");
                                return null;
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
                string sqlCheckStatus = @"SELECT IsOn FROM Logger WHERE ServerID = $ServerID LIMIT 1"; // check state
                using (SqliteCommand command = new SqliteCommand(sqlCheckStatus, dbConnection))
                {
                    command.Parameters.Add(new SqliteParameter("$ServerID", guild.Id));
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

            public async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
            {
                // handle here
            }
            public async Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel channel)
            {
                var message = await before.GetOrDownloadAsync();
                Console.WriteLine($"deleted message: {message}");
            }
            public async Task UserBanned(IUser user, IGuild guild)
            {
                // check toggle - if off then return
                // get channel - if false then return
                 var embed = new EmbedBuilder()
                 .WithTitle($"🔨 User Banned | {user.Username}")
                 .WithColor(Color.Red)
                 .WithDescription($"{user.Username} | ({user.Id})")
                 .WithFooter(DateTime.Now.ToString());

                 if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                     embed.WithThumbnailUrl(user.GetAvatarUrl());
            }
            public async Task UserUnbanned(IUser user, IGuild guild)
            {
                var embed = new EmbedBuilder()
                .WithTitle($"♻️ User Unbanned | {user.Username}")
                .WithColor(Color.Gold)
                .WithDescription($"{user.Username} | ({user.Id})")
                .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());
            }
            public async Task UserJoined(IGuildUser user)
            {
                 var embed = new EmbedBuilder()
                .WithTitle($"✅ User Joined | {user.Username}")
                .WithColor(Color.Green)
                .WithDescription($"{user.Username} | ({user.Id})")
                .AddField("Joined Server", user.JoinedAt)
                .AddField("Joined Discord", user.CreatedAt)
                .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());
            }
            public async Task UserLeft(IGuildUser user)
            {
                var embed = new EmbedBuilder()
                .WithTitle($"❎ User Left | {user.Username}")
                .WithColor(Color.Red)
                .WithDescription($"{user.Username} | ({user.Id})")
                .WithFooter(DateTime.Now.ToString());

                if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                    embed.WithThumbnailUrl(user.GetAvatarUrl());
            }
            public async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
            {
                // handle here
            }
            public async Task UserKicked(IUser user, IUser kicker)
            {
                // handle here
            }
            public async Task UserMuted(IUser user, IUser muter)
            {
                // handle here
            }
            public async Task UserUnmuted(IUser user, IUser unmuter)
            {
                // handle here
            }



        }
    }
}


