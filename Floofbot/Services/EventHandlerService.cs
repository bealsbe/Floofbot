using Discord;
using Discord.WebSocket;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Floofbot.Services
{
    public class EventHandlerService
    {
        private WordFilterService _wordFilterService;
        private NicknameAlertService _nicknameAlertService;
        private RaidProtectionService _raidProtectionService;
        private UserRoleRetentionService _userRoleRetentionService;
        private static readonly Color ADMIN_COLOR = Color.DarkOrange;

        // List of announcement channels
        private List<ulong> _announcementChannels;

        // Rules gate config
        private Dictionary <string, string> _rulesGateConfig = BotConfigFactory.Config.RulesGate;
        
        public EventHandlerService(DiscordSocketClient clientParam)
        {
            // We don't need a global private variable if we ONLY use it in this method!
            var client = clientParam;

            // Assisting handlers
            _wordFilterService = new WordFilterService();
            _nicknameAlertService = new NicknameAlertService(new FloofDataContext());
            _raidProtectionService = new RaidProtectionService();
            _userRoleRetentionService = new UserRoleRetentionService(new FloofDataContext());
            // Same rules as client, it's only used here, so we don't need a global variable for it!
            var welcomeGateService = new WelcomeGateService();

            // Event handlers
            client.MessageUpdated += MessageUpdated;
            client.MessageDeleted += MessageDeleted;
            client.UserBanned += UserBanned;
            client.UserUnbanned += UserUnbanned;
            client.UserJoined += UserJoined;
            client.UserLeft += UserLeft;
            client.GuildMemberUpdated += GuildMemberUpdated;
            client.GuildMemberUpdated += welcomeGateService.HandleWelcomeGate; // welcome gate handler
            client.UserUpdated += UserUpdated;
            client.MessageReceived += OnMessage;
            client.MessageReceived += RulesGate; // rfurry rules gate
            client.ReactionAdded += _nicknameAlertService.OnReactionAdded;

            // A list of announcement channels for auto publishing
            _announcementChannels = BotConfigFactory.Config.AnnouncementChannels;
        }

        private async Task PublishAnnouncementMessages(SocketUserMessage msg)
        {
            foreach (var chan in _announcementChannels)
            {
                if (msg.Channel.Id != chan) continue;
                
                if (msg.Channel.GetType() == typeof(SocketNewsChannel))
                {
                    await msg.CrosspostAsync();
                }
            }
        }

        private async Task<ITextChannel> GetChannel(IGuild guild, string eventName = null)
        {
            if (eventName == null)
                return null;

            await using (var floofDb = new FloofDataContext())
            {
                var serverConfig = await floofDb.LogConfigs.FindAsync(guild.Id);
            
                if (serverConfig == null) // guild not in database
                    return null;
            
                var propertyInfo = serverConfig.GetType().GetProperty(eventName);
                var logChannel = (ulong) propertyInfo.GetValue(serverConfig, null);
                var textChannel = await guild.GetTextChannelAsync(logChannel);
                
                return textChannel;
            }
        }

        private bool IsToggled(IGuild guild)
        {
            using (var floofDb = new FloofDataContext())
            {
                // Check if the logger is toggled on in this server
                // Check the status of logger
                var serverConfig = floofDb.LogConfigs.Find(guild.Id);
                
                return serverConfig != null && serverConfig.IsOn;
            }
        }
        
        private async Task CheckUserAutoban(IGuildUser user)
        {
            await using (var floofDb = new FloofDataContext())
            {
                var badUserAutoban = await floofDb.BansOnJoin.AsQueryable().FirstOrDefaultAsync(u => u.UserID == user.Id);
                
                if (badUserAutoban != null) // user is in the list to be autobanned
                {
                    //sends message to user
                    var builder = new EmbedBuilder
                    {
                        Title = "⚖️ Ban Notification",
                        Description = $"You have been automatically banned from {user.Guild.Name}",
                        Color = ADMIN_COLOR
                    }.AddField("Reason", badUserAutoban.Reason);
                    
                    await user.SendMessageAsync(string.Empty, false, builder.Build());
                    
                    await user.Guild.AddBanAsync(user.Id, 0,
                        $"{badUserAutoban.ModUsername} -> {badUserAutoban.Reason} (autobanned on user join)");

                    try
                    {
                        floofDb.Remove(badUserAutoban);
                        
                        await floofDb.SaveChangesAsync();
                    }
                    catch (Exception e) // db error
                    {
                        Log.Error("Error with the auto ban on join system: " + e);
                    }
                }
            }
        }

        // r/Furry rules gate
        private async Task RulesGate(SocketMessage msg)
        {
            var userMsg = msg as SocketUserMessage;
            
            if (msg == null || msg.Author.IsBot)
                return;

            // Rules gate info
            var rulesChannelId = Convert.ToUInt64(_rulesGateConfig["RulesChannel"]);
            var readRulesRoleId = Convert.ToUInt64(_rulesGateConfig["RulesRole"]);
            var rulesBypassString = _rulesGateConfig["Keyword"];

            if (msg.Channel.Id == rulesChannelId && userMsg.Content.ToLower().Contains(rulesBypassString)) 
            {
                var user = (IGuildUser)msg.Author;
                
                await user.AddRoleAsync(user.Guild.GetRole(readRulesRoleId));
                await userMsg.DeleteAsync();
            }
        }

        private Task OnMessage(SocketMessage msg)
        {
            if (msg.Channel.GetType() == typeof(SocketDMChannel))
                return Task.CompletedTask;

            var userMsg = msg as SocketUserMessage;
            
            Task.Run(async () =>
            {
                try
                {
                    // handle announcement messages
                    await PublishAnnouncementMessages(userMsg);

                    if (msg == null || msg.Author.IsBot)
                        return;

                    var messageTriggeredRaidProtection = _raidProtectionService.CheckMessage(new FloofDataContext(), msg).Result;
                    
                    if (messageTriggeredRaidProtection)
                    {
                        await msg.DeleteAsync();
                        return;
                    }

                    var randomResponse = RandomResponseGenerator.GenerateResponse(userMsg);
                    
                    if (!string.IsNullOrEmpty(randomResponse))
                    {
                        await msg.Channel.SendMessageAsync(randomResponse);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("Error with the on message event handler: " + e);
                }
            });
            return Task.CompletedTask;
        }

        private Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel chan)
        {
            Task.Run(async () =>
            {
                try
                {
                    // Deal with empty message
                    var messageBefore = (before.HasValue ? before.Value : null) as IUserMessage;
                    
                    if (messageBefore == null)
                        return;

                    if (after.Author.IsBot)
                        return;

                    var channel = chan as ITextChannel; // Channel null, DM message?
                    
                    if (channel == null)
                        return;

                    if (messageBefore.Content == after.Content) // no change
                        return;

                    var messageTriggeredRaidProtection = _raidProtectionService.CheckMessage(new FloofDataContext(), after).Result;
                    
                    if (messageTriggeredRaidProtection)
                    {
                        await after.DeleteAsync();
                        return;
                    }

                    var hasBadWord = _wordFilterService.HasFilteredWord(new FloofDataContext(), after.Content, channel.Guild.Id, after.Channel.Id);
                    
                    if (hasBadWord)
                    {
                        await after.DeleteAsync();
                        
                        var botMsg = await after.Channel.SendMessageAsync($"{after.Author.Mention} There was a filtered word in that message. Please be mindful of your language!");
                        
                        await Task.Delay(5000);
                        await botMsg.DeleteAsync();
                    }

                    if (IsToggled(channel.Guild) == false) // Not toggled on
                        return;

                    var logChannel = await GetChannel(channel.Guild, "MessageUpdatedChannel");
                    
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

                    await logChannel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the message updated event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task MessageDeleted(Cacheable<IMessage, ulong> before, ISocketMessageChannel chan)
        {
            Task.Run(async () =>
            {
                try
                {
                    // deal with empty message
                    var message = (before.HasValue ? before.Value : null) as IUserMessage;
                    
                    if (message == null)
                        return;

                    if (message.Author.IsBot)
                        return;

                    var channel = chan as ITextChannel; // Channel null, DM message?
                    
                    if (channel == null)
                        return;

                    if (IsToggled(channel.Guild) == false) // not toggled on
                        return;
                    
                    var logChannel = await GetChannel(channel.Guild, "MessageDeletedChannel");
                    
                    if (logChannel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"⚠️ Message Deleted | {message.Author.Username}#{message.Author.Discriminator}")
                        .WithColor(Color.Gold)
                        .WithDescription($"{message.Author.Mention} ({message.Author.Id}) has had their message deleted in {channel.Mention}!")
                        .WithCurrentTimestamp()
                        .WithFooter($"user_message_deleted user_messagelog {message.Author.Id}");
                    
                    if (message.Content.Length > 0) 
                        embed.AddField("Content", message.Content);

                    if (message.Attachments.Count > 0)
                        embed.AddField("Attachments", String.Join("\n", message.Attachments.Select(it => it.Url)));

                    if (Uri.IsWellFormedUriString(message.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(message.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the message deleted event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }
        
        public Task MessageDeletedByBot(SocketMessage before, ITextChannel channel, string reason = "N/A")
        {
            Task.Run(async () =>
            {
                try
                {
                    if (before.Author.IsBot)
                        return;

                    // Deal with empty message
                    if (before.Content == null)
                        return;

                    if (channel == null)
                        return;

                    if (IsToggled(channel.Guild) == false) // not toggled on
                        return;

                    var logChannel = await GetChannel(channel.Guild, "MessageDeletedChannel");
                    
                    if (logChannel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"⚠️ Message Deleted By Bot | {before.Author.Username}#{before.Author.Discriminator}")
                        .WithColor(Color.Gold)
                        .WithDescription($"{before.Author.Mention} ({before.Author.Id}) has had their message deleted in {channel.Mention}!")
                        .AddField("Content", before.Content)
                        .AddField("Reason", reason)
                        .WithFooter($"user_message_bot_delete user_messagelog {before.Author.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(before.Author.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(before.Author.GetAvatarUrl());

                    await logChannel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the message deleted by bot event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task UserBanned(IUser user, IGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if (IsToggled(guild) == false)
                        return;

                    var channel = await GetChannel(guild, "UserBannedChannel");
                    
                    if (channel == null)
                        return;

                    var banReason = guild.GetBanAsync(user.Id).Result.Reason;

                    var embed = new EmbedBuilder()
                        .WithTitle($"🔨 User Banned | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Red)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .WithFooter($"user_banned user_banlog {user.Id}")
                        .WithCurrentTimestamp();

                    embed.AddField("Reason", banReason ?? "No Reason Provided");

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user banned event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task UserUnbanned(IUser user, IGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if (IsToggled(guild) == false)
                        return;

                    var channel = await GetChannel(guild, "UserUnbannedChannel");
                    
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"♻️ User Unbanned | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Gold)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .WithFooter($"user_unbanned user_banlog {user.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user unbanned event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task UserJoined(IGuildUser user)
        {
            Task.Run(async () =>
            {
                try
                {
                    await CheckUserAutoban(user); // Check if the user is to be automatically banned on join

                    if (user.IsBot)
                        return;

                    await _raidProtectionService.CheckForExcessiveJoins(user.Guild);
                    await _userRoleRetentionService.RestoreUserRoles(user);

                    if (IsToggled(user.Guild) == false)
                        return;

                    var channel = await GetChannel(user.Guild, "UserJoinedChannel");
                    
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"✅ User Joined | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Green)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .AddField("Joined Server", user.JoinedAt?.ToString("ddd, dd MMM yyyy"), true)
                        .AddField("Joined Discord", user.CreatedAt.ToString("ddd, dd MMM yyyy"), true)
                        .WithFooter($"user_join user_joinlog {user.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user joined event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task UserLeft(IGuildUser user)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    await _userRoleRetentionService.LogUserRoles(user);

                    if (IsToggled(user.Guild) == false)
                        return;

                    var channel = await GetChannel(user.Guild, "UserLeftChannel");
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"❌ User Left | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Red)
                        .WithDescription($"{user.Mention} | ``{user.Id}``");
                    
                    if (user.JoinedAt != null)
                    {
                        var userJoined = (DateTimeOffset)user.JoinedAt;
                        var interval = DateTime.UtcNow - userJoined.DateTime;
                        var dayWord = interval.Days == 1 ? "day" : "days";
                        
                        embed.AddField("Joined Server", userJoined.ToString("ddd, dd MMM yyyy"), true);
                        embed.AddField("Time at Server", $"{interval.Days} {dayWord}", true);
                    }
                    
                    embed.WithFooter($"user_leave user_joinlog {user.Id}")
                         .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user left event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task UserUpdated(SocketUser before, SocketUser userAfter)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (!(userAfter is SocketGuildUser after))
                        return;

                    if (before == null || after == null) // Empty user params
                        return;

                    if (after.IsBot)
                        return;

                    if (before.Username != after.Username)
                    {
                        var badWords = _wordFilterService.FilteredWordsInName(new FloofDataContext(), after.Username, after.Guild.Id);
                        
                        if (badWords != null)
                            await _nicknameAlertService.HandleBadNickname(after, after.Guild, badWords);
                    }

                    if (IsToggled(after.Guild) == false) // turned off
                        return;

                    var channel = await GetChannel(after.Guild, "MemberUpdatesChannel");
                    
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
                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user updated event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }

        private Task GuildMemberUpdated(SocketGuildUser before, SocketGuildUser after)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (before == null || after == null) // empty user params
                        return;

                    if (after.IsBot)
                        return;
                    
                    if (after.Nickname != null && after.Nickname != before.Nickname)
                    {
                        var badWords = _wordFilterService.FilteredWordsInName(new FloofDataContext(), after.Nickname, after.Guild.Id);
                        
                        if (badWords != null)
                            await _nicknameAlertService.HandleBadNickname(after, after.Guild, badWords);
                    }

                    if (IsToggled(after.Guild) == false) // turned off
                        return;

                    var channel = await GetChannel(after.Guild, "MemberUpdatesChannel");
                    
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
                        {
                            embed.AddField("Old Nickname", before.Nickname, true);
                        }
                        else // new nickname, didnt have one before
                        {
                            embed.AddField("New Nickname", after.Nickname, true);
                        }

                        if (Uri.IsWellFormedUriString(after.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(after.GetAvatarUrl());
                    }
                    else if (before.Roles.Count != after.Roles.Count)
                    {
                        var beforeRoles = new List<SocketRole>(before.Roles);
                        var afterRoles = new List<SocketRole>(after.Roles);
                        var roleDifference = new List<SocketRole>();

                        if (before.Roles.Count > after.Roles.Count) // roles removed
                        {
                            roleDifference = beforeRoles.Except(afterRoles).ToList();
                            
                            embed.WithTitle($"❗ Roles Removed | {after.Username}#{after.Discriminator}")
                                 .WithColor(Color.Orange)
                                 .WithDescription($"{after.Mention} | ``{after.Id}``")
                                 .WithFooter($"user_roles_removed user_rolelog {after.Id}")
                                 .WithCurrentTimestamp();

                            foreach (var role in roleDifference)
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
                            
                            foreach (var role in roleDifference)
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
                    
                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the guild member updated event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }
        
        public Task UserKicked(IUser user, IUser kicker, IGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if (IsToggled(guild) == false)
                        return;

                    var channel = await GetChannel(guild, "UserKickedChannel");
                    
                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"👢 User Kicked | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Red)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .AddField("Kicked By", kicker.Mention)
                        .WithFooter($"user_kicked {user.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user kicked event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }
        public Task UserMuted(IUser user, IUser muter, IGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;
                    
                    if (IsToggled(guild) == false)
                        return;

                    var channel = await GetChannel(guild, "UserMutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"🔇 User Muted | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Teal)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .AddField("Muted By", muter.Mention)
                        .WithFooter($"user_muted user_mutelog {user.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user muted event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }
        
        public Task UserUnmuted(IUser user, IUser unmuter, IGuild guild)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (user.IsBot)
                        return;

                    if (IsToggled(guild) == false)
                        return;

                    var channel = await GetChannel(guild, "UserUnmutedChannel");

                    if (channel == null)
                        return;

                    var embed = new EmbedBuilder()
                        .WithTitle($"🔊 User Unmuted | {user.Username}#{user.Discriminator}")
                        .WithColor(Color.Teal)
                        .WithDescription($"{user.Mention} | ``{user.Id}``")
                        .AddField("Unmuted By", unmuter.Mention)
                        .WithFooter($"user_unmuted user_mutelog {user.Id}")
                        .WithCurrentTimestamp();

                    if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                        embed.WithThumbnailUrl(user.GetAvatarUrl());

                    await channel.SendMessageAsync(string.Empty, false, embed.Build());
                }
                catch (Exception e)
                {
                    Log.Error("Error with the user unmuted event handler: " + e);
                }
            });
            
            return Task.CompletedTask;
        }
    }
}
