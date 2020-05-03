using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Microsoft.Data.Sqlite;

namespace Floofbot.Services
{
    class VoiceChannelSync
    {
        private DiscordSocketClient _client;
        public SqliteConnection dbConnection;
        public VoiceChannelSync(DiscordSocketClient client)
        {
            _client = client;

            _client.UserVoiceStateUpdated += VoiceStateUpdated;
            _client.JoinedGuild += JoinGuild;

            dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder {
                DataSource = "mappings.db"
            }.ToString());

            if (!File.Exists("mappings.db")) {

                FileStream fs = File.Create("mappings.db");
                fs.Close();

                string sql = @"CREATE TABLE `Mappings`(`VoiceID` TEXT,`TextID` TEXT,`Guild` TEXT,PRIMARY KEY(`VoiceID`));";
                SqliteCommand command = new SqliteCommand(sql, dbConnection);
                dbConnection.Open();
                command.ExecuteNonQuery();
                dbConnection.Close();
            }
        }


        //Creates text channels for all voice channels upon joining a new server
        private async Task JoinGuild(SocketGuild guild)
        {
            Log($"Setting up Links for {guild.Name} - {guild.Id}");
            try {
                foreach (SocketVoiceChannel voiceChannel in guild.VoiceChannels)
                    await InsertChannel(voiceChannel);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
        }

        //creates a new channel and inserts the values into a sqllite database
        private async Task InsertChannel(SocketVoiceChannel voiceChannel)
        {

            ITextChannel textChannel = await voiceChannel.Guild.CreateTextChannelAsync($"{voiceChannel.Name} - Voice");
            await textChannel.ModifyAsync(channel => channel.Topic = $":speaker: {voiceChannel.Name}");

            Log($"Created new Text Channel in {textChannel.GuildId}: {voiceChannel.Id} -> {textChannel.Id}");

            string insert = @"INSERT INTO Mappings (VoiceID, TextID, Guild) VALUES ($VoiceID, $TextID, $Guild);";
            SqliteCommand command = new SqliteCommand(insert, dbConnection);
            command.Parameters.Add(new SqliteParameter("$VoiceID", voiceChannel.Id));
            command.Parameters.Add(new SqliteParameter("$TextID", textChannel.Id));
            command.Parameters.Add(new SqliteParameter("$Guild", voiceChannel.Guild.Id));

            dbConnection.Open();
            command.ExecuteScalar();
            dbConnection.Close();

            OverwritePermissions overwrite = new OverwritePermissions(viewChannel: PermValue.Deny);
            await textChannel.AddPermissionOverwriteAsync(voiceChannel.Guild.EveryoneRole, overwrite);


        }

        //gets the TextChannelID from the VoiceChannel
        private async Task<ulong> GetChannelLink(SocketVoiceChannel voiceChannel)
        {
            ulong value;
            string select = "SELECT textID FROM Mappings WHERE VoiceID = $VoiceID";
            SqliteCommand command = new SqliteCommand(select, dbConnection);
            command.Parameters.Add(new SqliteParameter("$VoiceID", voiceChannel.Id));
            dbConnection.Open();
            var result = command.ExecuteScalar();
            dbConnection.Close();

            if (result == null) {
                InsertChannel(voiceChannel).GetAwaiter().GetResult();
                value = await GetChannelLink(voiceChannel);

            }
            else {
                value = ulong.Parse((string)result);
            }
            return value;
        }

        private async Task VoiceStateUpdated(SocketUser user, SocketVoiceState before, SocketVoiceState after)
        {
            //user leaves voice
            if (after.VoiceChannel == null) {
                await HandleDisconnect(user, (ISocketMessageChannel)_client.GetChannel(await GetChannelLink(before.VoiceChannel)));
            }
            //user joins voice
            else if (before.VoiceChannel == null) {
                await HandleConnect(user, (ISocketMessageChannel)_client.GetChannel(await GetChannelLink(after.VoiceChannel)));
            }
            //user swiches voice channels
            else if (before.VoiceChannel != after.VoiceChannel) {
                await HandleDisconnect(user, (ISocketMessageChannel)_client.GetChannel(await GetChannelLink(before.VoiceChannel)));
                await HandleConnect(user, (ISocketMessageChannel)_client.GetChannel(await GetChannelLink(after.VoiceChannel)));
            }

        }

        //updates channel permissions when a user joins a voice channel
        public async Task HandleConnect(SocketUser socketUser, ISocketMessageChannel targetChannel)
        {
            OverwritePermissions overwrite = new OverwritePermissions(viewChannel: PermValue.Allow);
            IGuildChannel textChannel = (IGuildChannel)targetChannel;

            await textChannel.AddPermissionOverwriteAsync(socketUser, overwrite);
            EmbedBuilder builder = new EmbedBuilder() {
                Description = ":speaker: " + socketUser.Mention + " has joined the voice channel",
                Color = Color.Green,
                ThumbnailUrl = socketUser.GetAvatarUrl(ImageFormat.Auto, 128)
            };
            await targetChannel.SendMessageAsync("" , false , builder.Build());
        }

        //updates channel permissions when a user leaves a voice channel
        public async Task HandleDisconnect(SocketUser socketUser, ISocketMessageChannel targetChannel)
        {
            // Don't hide the channel for users that can manage messages
            if (!((SocketGuildUser)socketUser).GuildPermissions.ManageMessages) {
                OverwritePermissions overwrite = new OverwritePermissions(viewChannel: PermValue.Deny);
                IGuildChannel textChannel = (IGuildChannel)targetChannel;
                await textChannel.AddPermissionOverwriteAsync(socketUser, overwrite);
            }
            EmbedBuilder builder = new EmbedBuilder() {
                Description = ":mute: " + socketUser.Mention + " has left the voice channel  ",
                Color = Color.Orange,
                ThumbnailUrl = socketUser.GetAvatarUrl(ImageFormat.Auto, 128)
            };

            await targetChannel.SendMessageAsync("", false, builder.Build());
        }

        private void Log(string message) => Console.WriteLine($"{DateTime.Now.ToString("[MM/dd/yyyy HH:mm]")} ChannelLink: {message}");


    }
}
