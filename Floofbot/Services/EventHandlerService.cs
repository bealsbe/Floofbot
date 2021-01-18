using Discord;
using Discord.WebSocket;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    public class EventHandlerService
    {

        private DiscordSocketClient _client;
        private WordFilterService _wordFilterService;
        private NicknameAlertService _nicknameAlertService;
        private RaidProtectionService _raidProtectionService;
        private UserRoleRetentionService _userRoleRetentionService;
        private static readonly Color ADMIN_COLOR = Color.DarkOrange;

        // list of announcement channels
        private List<ulong> announcementChannels;

        // rules gate config
        private Dictionary <string, string> rulesGateConfig = BotConfigFactory.Config.RulesGate;


        public EventHandlerService(DiscordSocketClient client)
        {
            _client = client;

            // assisting handlers
            _wordFilterService = new WordFilterService();
            _nicknameAlertService = new NicknameAlertService(new FloofDataContext());
            _raidProtectionService = new RaidProtectionService();
            _userRoleRetentionService = new UserRoleRetentionService(new FloofDataContext());

            // event handlers
            _client.MessageUpdated += MessageUpdated;
            _client.MessageDeleted += MessageDeleted;
            _client.UserBanned += UserBanned;
            _client.UserUnbanned += UserUnbanned;
            _client.UserJoined += UserJoined;
            _client.UserJoined += _userRoleRetentionService.LogUserRoles;
            _client.UserLeft += _userRoleRetentionService.RemoveUserRoleLog;
            _client.UserLeft += UserLeft;
            _client.GuildMemberUpdated += GuildMemberUpdated;
            _client.UserUpdated += UserUpdated;
            _client.MessageReceived += OnMessage;
            _client.MessageReceived += RulesGate; // rfurry rules gate
            _client.ReactionAdded += _nicknameAlertService.OnReactionAdded;

            // a list of announcement channels for auto publishing
            announcementChannels = BotConfigFactory.Config.AnnouncementChannels;
        }
        public async Task PublishAnnouncementMessages(SocketUserMessage msg)
        {
            foreach (ulong chan in announcementChannels)
            {
                if (msg.Channel.Id == chan)
                {
                    if (msg.Channel.GetType() == typeof(SocketNewsChannel))
                    {
                        await msg.CrosspostAsync();
                    }
                }
            }
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

        // rfurry rules gate
        public async Task RulesGate(SocketMessage msg)
        {
            var userMsg = msg as SocketUserMessage;
            if (msg == null || msg.Author.IsBot)
                return;

            // rules gate info
            ulong rulesChannelId = Convert.ToUInt64(rulesGateConfig["RulesChannel"]);
            ulong readRulesRoleId = Convert.ToUInt64(rulesGateConfig["RulesRole"]);
            string rulesBypassString = rulesGateConfig["Keyword"];

            if (msg.Channel.Id == rulesChannelId && userMsg.Content.ToLower().Contains(rulesBypassString)) 
            {
                var user = (IGuildUser)msg.Author;
                await user.AddRoleAsync(user.Guild.GetRole(readRulesRoleId));
                await userMsg.DeleteAsync();
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
                    // handle announcement messages
                    await PublishAnnouncementMessages(userMsg);

                    if (msg == null || msg.Author.IsBot)
                        return;
                    var channel = msg.Channel as ITextChannel;

                    bool messageTriggeredRaidProtection = _raidProtectionService.CheckMessage(new FloofDataContext(), msg).Result;
                    if (messageTriggeredRaidProtection)
                    {
                        await msg.DeleteAsync();
                        return;
                    }

                    string randomResponse = RandomResponseGenerator.GenerateResponse(userMsg);
                    if (!string.IsNullOrEmpty(randomResponse))
                    {
                        await msg.Channel.SendMessageAsync(randomResponse);
                        return;
                    }
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
                    {
                        await after.DeleteAsync();
                        var botMsg = await after.Channel.SendMessageAsync($"{after.Author.Mention} There was a filtered word in that message. Please be mindful of your language!");
                        await Task.Delay(5000);
                        await botMsg.DeleteAsync();
                    }

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
                         .WithCurrentTimestamp()
                         .WithFooter($"user_message_deleted user_messagelog {message.Author.Id}");
                    if (message.Content.Length > 0)
                    {
                        embed.AddField("Content", message.Content);
                    }
                    if (message.Attachments.Count > 0)
                    {
                        embed.AddField("Attachments", String.Join("\n", message.Attachments.Select(it => it.Url)));
                    }

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
        public Task UserUpdated(SocketUser before, SocketUser userAfter)
        {

            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(userAfter is SocketGuildUser after))
                        return;

                    if (before == null || after == null) // empty user params
                        return;

                    if (after.IsBot)
                        return;

                    if (IsToggled(after.Guild) == false) // turned off
                        return;

                    Discord.ITextChannel channel = await GetChannel(after.Guild, "MemberUpdatesChannel");
                    if (channel == null) // no log channel set
                        return;

                    var embed = new EmbedBuilder();

                    if (before.Username != after.Username)
                    {
                        embed.WithTitle($"👥 Username Changed | {after.Username}#{after.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{after.Mention} | ``{after.Id}``")
                             .AddField("Old Username", before.Username, true)
                             .AddField("New Name", after.Username, true)
                             .WithFooter($"user_username_change user_namelog {after.Id}")
                             .WithCurrentTimestamp();

                        if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(after.GetAvatarUrl());

                        bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), after.Username, channel.Guild.Id);
                        if (hasBadWord)
                            await _nicknameAlertService.HandleBadNickname(after, after.Guild);

                    }
                    else if (before.AvatarId != after.AvatarId)
                    {
                        embed.WithTitle($"🖼️ Avatar Changed | {after.Username}#{after.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{after.Mention} | ``{after.Id}``")
                             .WithFooter($"user_avatar_change {after.Id}")
                             .WithCurrentTimestamp();
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
                catch (Exception ex)
                {
                    Log.Error("Error with the user updated event handler: " + ex);
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

                    if (IsToggled(after.Guild) == false) // turned off
                        return;

                    Discord.ITextChannel channel = await GetChannel(after.Guild, "MemberUpdatesChannel");
                    if (channel == null) // no log channel set
                        return;

                    var embed = new EmbedBuilder();

                    if (before.Nickname != after.Nickname)
                    {
                        embed.WithTitle($"👥 Nickname Changed | {after.Username}#{after.Discriminator}")
                             .WithColor(Color.Purple)
                             .WithDescription($"{after.Mention} | ``{after.Id}``")
                             .WithFooter($"user_nickname_change user_namelog {after.Id}")
                             .WithCurrentTimestamp();

                        if (before.Nickname != null && after.Nickname != null) // changing nickname
                        {
                            embed.AddField("Old Nickname", before.Nickname, true);
                            embed.AddField("New Nickname", after.Nickname, true);
                        }
                        else if (after.Nickname == null) // removed their nickname
                            embed.AddField("Old Nickname", before.Nickname, true);
                        else // new nickname, didnt have one before
                            embed.AddField("New Nickname", after.Nickname, true);

                        if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(after.GetAvatarUrl());
                        if (after.Nickname != null)
                        {
                            bool hasBadWord = _wordFilterService.hasFilteredWord(new FloofDataContext(), after.Nickname, channel.Guild.Id);
                            if (hasBadWord)
                                await _nicknameAlertService.HandleBadNickname(after, after.Guild);
                        }

                    }
                    else if (before.Roles.Count != after.Roles.Count)
                    {
                        List<SocketRole> beforeRoles = new List<SocketRole>(before.Roles);
                        List<SocketRole> afterRoles = new List<SocketRole>(after.Roles);
                        List<SocketRole> roleDifference = new List<SocketRole>();

                        if (before.Roles.Count > after.Roles.Count) // roles removed
                        {
                            roleDifference = beforeRoles.Except(afterRoles).ToList();
                            embed.WithTitle($"❗ Roles Removed | {after.Username}#{after.Discriminator}")
                                 .WithColor(Color.Orange)
                                 .WithDescription($"{after.Mention} | ``{after.Id}``")
                                 .WithFooter($"user_roles_removed user_rolelog {after.Id}")
                                 .WithCurrentTimestamp();

                            foreach (SocketRole role in roleDifference)
                            {
                                embed.AddField("Role Removed", role);
                            }

                            if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                                embed.WithThumbnailUrl(after.GetAvatarUrl());
                        }
                        else if (before.Roles.Count < after.Roles.Count) // roles added
                        {
                            roleDifference = afterRoles.Except(beforeRoles).ToList();
                            embed.WithTitle($"❗ Roles Added | {after.Username}#{after.Discriminator}")
                                 .WithColor(Color.Orange)
                                 .WithDescription($"{after.Mention} | ``{after.Id}``")
                                 .WithFooter($"user_roles_added user_rolelog {after.Id}")
                                 .WithCurrentTimestamp();
                            foreach (SocketRole role in roleDifference)
                            {
                                embed.AddField("Role Added", role);
                            }
                            if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                                embed.WithThumbnailUrl(after.GetAvatarUrl());
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
    }
}
