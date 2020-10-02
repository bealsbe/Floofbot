using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    public class EventLoggerService
    {

        private DiscordSocketClient _client;
        private WordFilterService _wordFilterService;
        private NicknameAlertService _nicknameAlertService;
        private RaidProtectionService _raidProtectionService;
        private static readonly Color ADMIN_COLOR = Color.DarkOrange;


        public EventLoggerService(DiscordSocketClient client)
        {
            _client = client;

            // assisting handlers
            _wordFilterService = new WordFilterService();
            _nicknameAlertService = new NicknameAlertService(new FloofDataContext());
            _raidProtectionService = new RaidProtectionService();
            // event handlers
            _client.MessageUpdated += MessageUpdated;
            _client.MessageDeleted += MessageDeleted;
            _client.UserBanned += UserBanned;
            _client.UserUnbanned += UserUnbanned;
            _client.UserJoined += UserJoined;
            _client.UserLeft += UserLeft;
            _client.GuildMemberUpdated += GuildMemberUpdated;
            _client.MessageReceived += OnMessage;
            _client.ReactionAdded += _nicknameAlertService.OnReactionAdded;
        }
        public async Task<ITextChannel> GetChannel(Discord.IGuild guild, string eventName = null)
        {
            if (eventName == null)
                return null;

            FloofDataContext _floofDb = new FloofDataContext();

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
            FloofDataContext _floofDb = new FloofDataContext();

            var ServerConfig = _floofDb.LogConfigs.Find(guild.Id);
            if (ServerConfig == null) // no entry in DB for server - not configured
                return false;

            return ServerConfig.IsOn;
        }
        private async Task HandleBadMessage(SocketUser user, SocketMessage msg)
        {
            await msg.DeleteAsync();
            var botMsg = await msg.Channel.SendMessageAsync($"{user.Mention} watch your language! You've said a bad word!");
            await Task.Delay(5000);
            await botMsg.DeleteAsync();
        }
        private async Task CheckUserAutoban(IGuildUser user)
        {
            FloofDataContext _floofDb = new FloofDataContext();
            BanOnJoin badUserAutoban = _floofDb.BansOnJoin.AsQueryable().Where(u => u.UserID == user.Id).FirstOrDefault();
            if (badUserAutoban != null) // user is in the list to be autobanned
            {
                //sends message to user
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "⚖️ Ban Notification";
                builder.Description = $"You have been automatically banned from {user.Guild.Name}";
                builder.AddField("Reason", badUserAutoban.Reason);
                builder.Color = ADMIN_COLOR;
                await user.SendMessageAsync("", false, builder.Build());
                await user.Guild.AddBanAsync(user.Id, 0, $"{badUserAutoban.ModUsername} -> {badUserAutoban.Reason} (autobanned on user join)");

                try
                {
                    _floofDb.Remove(badUserAutoban);
                    await _floofDb.SaveChangesAsync();
                    return;
                }
                catch (Exception ex) // db error
                {
                    Log.Error("Error with the auto ban on join system: " + ex);
                    return;
                }
            }
        }
        public Task OnMessage(SocketMessage msg)
        {
            if (msg.Channel.GetType() == typeof(SocketDMChannel))
                return Task.CompletedTask;

            var userMsg = msg as SocketUserMessage;
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (msg == null || msg.Author.IsBot)
                        return;
                    var channel = msg.Channel as ITextChannel;

                    bool messageTriggeredRaidProtection = _raidProtectionService.CheckMessage(new FloofDataContext(), msg).Result;
                    if (messageTriggeredRaidProtection)
                    {
                        await msg.DeleteAsync();
                        return;
                    }
                  
                    bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), msg.Content, channel.Guild.Id, msg.Channel.Id);
                    if (hasBadWord)
                        await HandleBadMessage(msg.Author, msg);
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the on message event handler: " + ex);
                    return;
                }
            });
            return Task.CompletedTask;
        }
        public Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel chan)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    // deal with empty message
                    var messageBefore = (before.HasValue ? before.Value : null) as IUserMessage;
                    if (messageBefore == null)
                        return;

                    if (after.Author.IsBot)
                        return;

                    var channel = chan as ITextChannel; // channel null, dm message?
                    if (channel == null)
                        return;

                    if (messageBefore.Content == after.Content) // no change
                        return;

                    bool messageTriggeredRaidProtection = _raidProtectionService.CheckMessage(new FloofDataContext(), after).Result;
                    if (messageTriggeredRaidProtection)
                    {
                        await after.DeleteAsync();
                        return;
                    }

                    bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), after.Content, channel.Guild.Id, after.Channel.Id);
                    if (hasBadWord)
                        await HandleBadMessage(after.Author, after);

                    if ((IsToggled(channel.Guild)) == false) // not toggled on
                        return;

                    Discord.ITextChannel logChannel = await GetChannel(channel.Guild, "MessageUpdatedChannel");
                    if (logChannel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"⚠️ Message Edited | {after.Author.Username}#{after.Author.Discriminator}")
                         .WithColor(Color.DarkGrey)
                         .WithDescription($"{after.Author.Mention} ({after.Author.Id}) has edited their message in {channel.Mention}!")
                         .AddField("Before", messageBefore.Content, true)
                         .AddField("After", after.Content, true)
                         .WithCurrentTimestamp()
                         .WithFooter($"user_message_edited user_messagelog {after.Author.Id}");

                    if (Uri.IsWellFormedUriString(after.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(after.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the message updated event handler: " + ex);
                    return;
                }
            });
            return Task.CompletedTask;
        }
        public Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel chan)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    // deal with empty message
                    var message = (before.HasValue ? before.Value : null) as IUserMessage;
                    if (message == null)
                        return;

                    if (message.Author.IsBot)
                        return;

                    var channel = chan as ITextChannel; // channel null, dm message?
                    if (channel == null)
                        return;

                    if ((IsToggled(channel.Guild)) == false) // not toggled on
                        return;


                    Discord.ITextChannel logChannel = await GetChannel(channel.Guild, "MessageDeletedChannel");
                    if (logChannel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"⚠️ Message Deleted | {message.Author.Username}#{message.Author.Discriminator}")
                         .WithColor(Color.Gold)
                         .WithDescription($"{message.Author.Mention} ({message.Author.Id}) has had their message deleted in {channel.Mention}!")
                         .AddField("Content", message.Content)
                         .WithCurrentTimestamp()
                         .WithFooter($"user_message_deleted user_messagelog {message.Author.Id}");

                    if (Uri.IsWellFormedUriString(message.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(message.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync("", false, embed.Build());
                }
                catch (Exception ex)
                {
                    Log.Error("Error with the message deleted event handler: " + ex);
                    return;
                }
            });
            return Task.CompletedTask;
        }
        public Task MessageDeletedByBot(SocketMessage before, ITextChannel channel, string reason = "N/A")
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (before.Author.IsBot)
                        return;

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

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"⚠️ Message Deleted By Bot | {before.Author.Username}#{before.Author.Discriminator}")
                         .WithColor(Color.Gold)
                         .WithDescription($"{before.Author.Mention} ({before.Author.Id}) has had their message deleted in {channel.Mention}!")
                         .AddField("Content", before.Content)
                         .AddField("Reason", reason)
                         .WithFooter($"user_message_bot_delete user_messagelog {before.Author.Id}")
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
            });
            return Task.CompletedTask;
        }
        public Task UserBanned(IUser user, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserBannedChannel");
                    if (channel == null)
                        return;

                    var banReason = guild.GetBanAsync(user.Id).Result.Reason;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"🔨 User Banned | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Red)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .WithFooter($"user_banned user_banlog {user.Id}")
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
            });
            return Task.CompletedTask;

        }
        public Task UserUnbanned(IUser user, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserUnbannedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"♻️ User Unbanned | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Gold)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .WithFooter($"user_unbanned user_banlog {user.Id}")
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
            });
            return Task.CompletedTask;

        }
        public Task UserJoined(IGuildUser user)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    await CheckUserAutoban(user); // check if the user is to be automatically banned on join

                    if (user.IsBot)
                        return;

                    await _raidProtectionService.CheckForExcessiveJoins(user.Guild);

                    if ((IsToggled(user.Guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(user.Guild, "UserJoinedChannel");
                    if (channel == null)
                        return;


                    var embed = new EmbedBuilder();

                    embed.WithTitle($"✅ User Joined | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Green)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .AddField("Joined Server", user.JoinedAt?.ToString("ddd, dd MMM yyyy"), true)
                         .AddField("Joined Discord", user.CreatedAt.ToString("ddd, dd MMM yyyy"), true)
                         .WithFooter($"user_join user_joinlog {user.Id}")
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
            });
            return Task.CompletedTask;
        }
        public Task UserLeft(IGuildUser user)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if ((IsToggled(user.Guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(user.Guild, "UserLeftChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder();
                    embed.WithTitle($"❌ User Left | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Red)
                         .WithDescription($"{user.Mention} | ``{user.Id}``");
                    if (user.JoinedAt != null)
                    {
                        DateTimeOffset userJoined = ((DateTimeOffset)user.JoinedAt);
                        TimeSpan interval = DateTime.UtcNow - userJoined.DateTime;
                        string day_word = interval.Days == 1 ? "day" : "days";
                        embed.AddField("Joined Server", userJoined.ToString("ddd, dd MMM yyyy"), true);
                        embed.AddField("Time at Server", $"{interval.Days} {day_word}", true);
                    }
                    embed.WithFooter($"user_leave user_joinlog {user.Id}")
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
            });
            return Task.CompletedTask;

        }
        public Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (before == null || after == null) // empty user params
                        return;

                    if (after.IsBot)
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
                        embed.WithTitle($"👥 Username Changed | {user.Username}#{user.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{user.Mention} | ``{user.Id}``")
                             .AddField("Old Username", user.Username, true)
                             .AddField("New Name", user.Username, true)
                             .WithFooter($"user_username_change user_namelog {user.Id}")
                             .WithCurrentTimestamp();

                        if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(user.GetAvatarUrl());

                        bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), after.Username, channel.Guild.Id);
                        if (hasBadWord)
                            await _nicknameAlertService.HandleBadNickname(user, user.Guild);

                    }
                    else if (before.Nickname != after.Nickname)
                    {
                        embed.WithTitle($"👥 Nickname Changed | {user.Username}#{user.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{user.Mention} | ``{user.Id}``")
                             .WithFooter($"user_nickname_change user_namelog {user.Id}")
                             .WithCurrentTimestamp();

                        if (before.Nickname != null && after.Nickname != null) // changing nickname
                        {
                            embed.AddField("Old Nickname", before.Nickname, true);
                            embed.AddField("New Nickname", user.Nickname, true);
                        }
                        else if (after.Nickname == null) // removed their nickname
                            embed.AddField("Old Nickname", before.Nickname, true);
                        else // new nickname, didnt have one before
                            embed.AddField("New Nickname", user.Nickname, true);

                        if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(user.GetAvatarUrl());
                        if (after.Nickname != null)
                        {
                            bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), after.Nickname, channel.Guild.Id);
                            if (hasBadWord)
                                await _nicknameAlertService.HandleBadNickname(user, user.Guild);
                        }

                    }
                    else if (before.AvatarId != after.AvatarId)
                    {
                        embed.WithTitle($"🖼️ Avatar Changed | {user.Username}#{user.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{user.Mention} | ``{user.Id}``")
                             .WithFooter($"user_avatar_change {user.Id}")
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
                            embed.WithTitle($"❗ Roles Removed | {user.Username}#{user.Discriminator}")
                                 .WithColor(Color.Orange)
                                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                                 .WithFooter($"user_roles_removed user_rolelog {user.Id}")
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
                            embed.WithTitle($"❗ Roles Added | {user.Username}#{user.Discriminator}")
                                 .WithColor(Color.Orange)
                                 .WithDescription($"{user.Mention} | ``{user.Id}``")
                                 .WithFooter($"user_roles_added user_rolelog {user.Id}")
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
            });
            return Task.CompletedTask;

        }
        public Task UserKicked(IUser user, IUser kicker, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserKickedChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"👢 User Kicked | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Red)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .AddField("Kicked By", kicker.Mention)
                         .WithFooter($"user_kicked {user.Id}")
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
            });
            return Task.CompletedTask;
        }
        public Task UserMuted(IUser user, IUser muter, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;
                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserMutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"🔇 User Muted | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Teal)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .AddField("Muted By", muter.Mention)
                         .WithFooter($"user_muted user_mutelog {user.Id}")
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
            });
            return Task.CompletedTask;
        }
        public Task UserUnmuted(IUser user, IUser unmuter, IGuild guild)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if ((IsToggled(guild)) == false)
                        return;

                    Discord.ITextChannel channel = await GetChannel(guild, "UserUnmutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder();

                    embed.WithTitle($"🔊 User Unmuted | {user.Username}#{user.Discriminator}")
                         .WithColor(Color.Teal)
                         .WithDescription($"{user.Mention} | ``{user.Id}``")
                         .AddField("Unmuted By", unmuter.Mention)
                         .WithFooter($"user_unmuted user_mutelog {user.Id}")
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
            });
            return Task.CompletedTask;
        }

        class WordFilterService
        {
            List<FilteredWord> _filteredWords;
            DateTime _lastRefreshedTime;

            public bool hasFilteredWord(FloofDataContext floofDb, string messageContent, ulong serverId) // names
            {
                // return false if none of the serverIds match or filtering has been disabled for the server
                if (!floofDb.FilterConfigs.AsQueryable()
                    .Any(x => x.ServerId == serverId && x.IsOn))
                {
                    return false;
                }

                DateTime currentTime = DateTime.Now;
                if (_lastRefreshedTime == null || currentTime.Subtract(_lastRefreshedTime).TotalMinutes >= 30)
                {
                    _filteredWords = floofDb.FilteredWords.AsQueryable()
                        .Where(x => x.ServerId == serverId).ToList();
                    _lastRefreshedTime = currentTime;
                }

                foreach (var filteredWord in _filteredWords)
                {
                    Regex r = new Regex($"{filteredWord.Word}",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (r.IsMatch(messageContent))
                    {
                        return true;
                    }
                }
                return false;
            }
            public bool hasFilteredWord(FloofDataContext floofDb, string messageContent, ulong serverId, ulong channelId) // messages
            {
                // return false if none of the serverIds match or filtering has been disabled for the server
                if (!floofDb.FilterConfigs.AsQueryable()
                    .Any(x => x.ServerId == serverId && x.IsOn))
                {
                    return false;
                }

                // whitelist means we don't have the filter on for this channel
                if (floofDb.FilterChannelWhitelists.AsQueryable()
                    .Any(x => x.ChannelId == channelId && x.ServerId == serverId))
                {
                    return false;
                }

                DateTime currentTime = DateTime.Now;
                if (_lastRefreshedTime == null || currentTime.Subtract(_lastRefreshedTime).TotalMinutes >= 30)
                {
                    _filteredWords = floofDb.FilteredWords.AsQueryable()
                        .Where(x => x.ServerId == serverId).ToList();
                    _lastRefreshedTime = currentTime;
                }

                foreach (var filteredWord in _filteredWords)
                {
                    Regex r = new Regex(@$"\b({filteredWord.Word})\b",
                        RegexOptions.IgnoreCase | RegexOptions.Singleline);
                    if (r.IsMatch(messageContent))
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
