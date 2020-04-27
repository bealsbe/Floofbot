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
        private DiscordSocketClient client;

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
                            'ChannelID' INTEGER DEFAULT 0 NOT NULL,
                            'IsOn' INT DEFAULT 0 NOT NULL)
                      ";
                using (SqliteCommand cmdCreateStructure = new SqliteCommand(sqlCreateStructure, dbConnection))
                {
                    cmdCreateStructure.ExecuteScalar();
                }
            }

            [Command("setchannel")]
            public async Task channel(Discord.IChannel channel)
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
                    cmdInsertServer.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                    cmdInsertServer.ExecuteNonQuery();
                }

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
            public async Task toggle()
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
                    cmdInsertServer.Parameters.Add(new SqliteParameter("$ServerID", Context.Guild.Id));
                    cmdInsertServer.ExecuteNonQuery();
                }

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
                                long channelId = (long)result["ChannelID"];
                                if (channelId == 0) // they havent set channel yet :(
                                {
                                    await Context.Channel.SendMessageAsync("You haven't set your channel yet! Run ``logger setchannel <channel>``.");
                                    return;
                                }
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

            protected async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)
            {
                // If the message was not in the cache, downloading it will result in getting a copy of after.
                var message = await before.GetOrDownloadAsync();
                Console.WriteLine($"{message} -> {after}");
            }
            protected async Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel channel)
            {
                var message = await before.GetOrDownloadAsync();
                Console.WriteLine($"deleted message: {message}");
            }
            protected async Task UserBanned(IUser user, IGuild guild)
            {
                // handle here
            }
            protected async Task UserUnbanned(IUser user, IGuild guild)
            {
                // handle here
            }
            protected async Task UserJoined(IGuildUser user)
            {

            }
            protected async Task UserLeft(IGuildUser user)
            {
                // handle here
            }
            protected async Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
            {
                // handle here
            }

        }
    }
}


