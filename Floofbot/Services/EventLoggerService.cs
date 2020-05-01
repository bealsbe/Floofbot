using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    public class EventLoggerService
    {

        private FloofDataContext _floofDb;
        private DiscordSocketClient _client;
        public EventLoggerService(FloofDataContext floofDb, DiscordSocketClient client)
        {

            _floofDb = floofDb;
            _client = client;
            _client.MessageUpdated += MessageUpdated;
            _client.MessageDeleted += MessageDeleted;
            _client.UserBanned += UserBanned;
            _client.UserUnbanned += UserUnbanned;
            _client.UserJoined +=UserJoined;
            _client.UserLeft += UserLeft;
            _client.GuildMemberUpdated += GuildMemberUpdated;
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
                if (logChannel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Edited | {after.Author.Username}")
                 .WithColor(Color.DarkGrey)
                 .WithDescription($"{after.Author.Mention} ({after.Author.Id}) has edited their message in {channel.Mention}!")
                 .AddField("Before", messageBefore.Content, true)
                 .AddField("After", after.Content, true)
                 .WithCurrentTimestamp();

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
                if (logChannel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Deleted | {message.Author.Username}")
                 .WithColor(Color.Gold)
                 .WithDescription($"{message.Author.Mention} ({message.Author.Id}) has had their message deleted in {channel.Mention}!")
                 .AddField("Content", message.Content)
                 .WithCurrentTimestamp(); 

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
                if (logChannel == null)
                    return;

                var embed = new EmbedBuilder()
                 .WithTitle($"⚠️ Message Deleted By Bot | {before.Author.Username}")
                 .WithColor(Color.Gold)
                 .WithDescription($"{before.Author.Mention} ({before.Author.Id}) has had their message deleted in {channel.Mention}!")
                 .AddField("Content", before.Content)
                 .AddField("Reason", reason)
                 .WithCurrentTimestamp();

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
                 .WithCurrentTimestamp();

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
                .WithCurrentTimestamp();

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
                .AddField("Joined Server", user.JoinedAt, true)
                .AddField("Joined Discord", user.CreatedAt, true)
                .WithCurrentTimestamp();

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
                .WithCurrentTimestamp();

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
                        .AddField("Old Username", user.Username, true)
                        .AddField("New Name", user.Username, true)
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                }
                else if (before.Nickname != after.Nickname)
                {
                    embed.WithTitle($"👥 Nickname Changed | {user.Username}")
                        .WithColor(Color.Purple)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .AddField("Old Nickname", before.Nickname, true)
                        .AddField("New Nickname", user.Nickname, true)
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                }
                else if (before.AvatarId != after.AvatarId)
                {
                    embed.WithTitle($"🖼️ Avatar Changed | {user.Username}")
                    .WithColor(Color.Purple)
                    .WithDescription($"{user.Mention} | ``{user.Id}``")
                    .WithCurrentTimestamp();
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
                        .WithCurrentTimestamp();

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
                        .WithCurrentTimestamp();
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
                 .WithCurrentTimestamp();

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
                 .WithCurrentTimestamp();

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
                 .WithCurrentTimestamp();

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