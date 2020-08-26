using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    public class RaidProtectionService
    {
        // will store user id and how many counts they've had
        private Dictionary<ulong, int> userPunishmentCount = new Dictionary<ulong, int>();
        // used to keep track of the number of messages a user sent for spam protection
        private Dictionary<ulong, int> userMessageCount = new Dictionary<ulong, int>();
        // a list of punished users used to detect any potential raids
        private List<SocketUser> punishedUsers = new List<SocketUser>();
        // the total number of mentions a user can have before taking action
        private static int maxMentionCount = 10;
        // determines how long before a user is forgiven for a punishment
        private static int forgivenDuration = 5 * 60 * 1000; // 5 min
        // determines the rate at which users can send messages, currently no more than x messages in y seconds
        private static int durationForMaxMessages = 5 * 1000; // 5 s
        // determines the max number of punishments a user can have before being punished
        private static int maxNumberOfPunishments = 3;
        // the delay before the bot msg is deleted in ms
        private static int botMessageDeletionDelay = 3000;
        // the duration before a user is removed from the list of punished users
        private static int removePunishedUserDelay = 30 * 60 * 1000; // 30 min
        // the number of punished users within a time frame before the mods are alerted of possible raids
        private static int maxNumberPunishedUsers = 3;
        // These are used to determine if there are an excessive number of joins in a short time frame
        private static int maxNumberOfJoins = 5;
        private static int userJoinsDelay = 2 * 60 * 1000; // 2 min
        private Dictionary<IGuild, int> numberOfJoins = new Dictionary<IGuild, int>();
        // The max number of repeated emojis before triggering the raid protection
        private static int maxNumberEmojis = 5;



        public RaidProtectionService()
        {

        }
        public RaidProtectionConfig GetServerConfig(IGuild guild, FloofDataContext _floofDb)
        {
            RaidProtectionConfig serverConfig = _floofDb.RaidProtectionConfigs.Find(guild.Id);
            return serverConfig;
        }
        public async Task NotifyModerators(SocketRole modRole, ITextChannel modChannel, string message)
        {
            await modChannel.SendMessageAsync(modRole.Mention + " there may be a possible raid! Reason: ``" + message + "``");
        }
        private async void UserPunishmentTimeout(ulong userid)
        {
            await Task.Delay(forgivenDuration);
            if (userPunishmentCount.ContainsKey(userid) && userPunishmentCount[userid] != 0)
            {
                userPunishmentCount[userid] -= 1;
                if (userPunishmentCount[userid] == 0)
                    userPunishmentCount.Remove(userid);
            }
        }
        private async void punishedUsersTimeout(SocketUser user)
        {
            await Task.Delay(removePunishedUserDelay);
            if (punishedUsers.Contains(user))
                punishedUsers.Remove(user);
        }
        private async void userJoinTimeout(IGuild guild)
        {
            await Task.Delay(userJoinsDelay);
            if (numberOfJoins.ContainsKey(guild) && numberOfJoins[guild] != 0)
            {
                numberOfJoins[guild] -= 1;
                if (numberOfJoins[guild] == 0)
                    numberOfJoins.Remove(guild);
            }
                        
        }
        private async void UserMessageCountTimeout(ulong userid)
        {
            await Task.Delay(durationForMaxMessages);
            if (userMessageCount.ContainsKey(userid))
                if (userMessageCount[userid] != 0)
                    userMessageCount[userid] -= 1;
        }
        public async Task<bool> CheckUserMessageCount(SocketMessage msg)
        {
            if (userMessageCount.ContainsKey(msg.Author.Id))
            {
                userMessageCount[msg.Author.Id] += 1;
                if (userMessageCount[msg.Author.Id] >= 5) // no more than 5 messages in time frame
                {
                    // add a bad boye point for the user
                    if (userPunishmentCount.ContainsKey(msg.Author.Id))
                    {
                        userPunishmentCount[msg.Author.Id] += 1;
                    }
                    else // they were a good boye but now they are not
                    {
                        userPunishmentCount.Add(msg.Author.Id, 1);
                    }
                    // reset the count after punishing the user
                    userMessageCount[msg.Author.Id] = 0;
                    // we run an async task to remove their point after the specified duration
                    await Task.Run(async () =>
                    {
                        await Task.Delay(forgivenDuration);

                        userPunishmentCount[msg.Author.Id] -= 1;
                    });
                    var botMsg = await msg.Channel.SendMessageAsync(msg.Author.Mention + " you are sending messages too quickly!");
                    await Task.Delay(botMessageDeletionDelay);
                    await botMsg.DeleteAsync();
                    return true;
                }
            }
            else // they were a good boye but now they are not
            {
                userMessageCount.Add(msg.Author.Id, 1);
            }
            // we run an async task to remove their point after the specified duration
            UserMessageCountTimeout(msg.Author.Id);
            return false;
        }
        public async Task<bool> CheckMentions(SocketUserMessage msg, IGuild guild, SocketRole modRole, ITextChannel modChannel)
        {
            if (msg.MentionedUsers.Count > maxMentionCount)
            {
                string reason = "Raid Protection =>" + msg.MentionedUsers.Count + " mentions in one message.";
                try
                {
                    //sends message to user
                    EmbedBuilder builder = new EmbedBuilder();
                    builder.Title = "⚖️ Ban Notification";
                    builder.Description = $"You have been banned from {guild.Name}";
                    builder.AddField("Reason", reason);
                    builder.Color = Discord.Color.Red;
                    await msg.Author.SendMessageAsync("", false, builder.Build());
                    await guild.AddBanAsync(msg.Author, 0, reason);

                    await NotifyModerators(modRole, modChannel, " I have banned a user for mentioning " + msg.MentionedUsers.Count() + " members");
                }
                catch (Exception e)
                {
                    Log.Error("Error banning user for mass mention: " + e);
                }
                return true;
            }
            return false;
        }
        public async Task<bool> CheckLetterSpam(SocketMessage msg)
        {
            var matches = Regex.Matches(msg.Content, @"(.)\1+");
            foreach (Match m in matches)
            {
                // string has more than 5 letters in a row
                if (m.Length > 5)
                {
                    // add a bad boye point for the user
                    if (userPunishmentCount.ContainsKey(msg.Author.Id))
                    {
                        userPunishmentCount[msg.Author.Id] += 1;
                    }
                    else // they were a good boye but now they are not
                    {
                        userPunishmentCount.Add(msg.Author.Id, 1);
                    }
                    // we run an async task to remove their point after the specified duration
                    UserPunishmentTimeout(msg.Author.Id);
                    var botMsg = await msg.Channel.SendMessageAsync(msg.Author.Mention + " no spamming!");
                    await Task.Delay(botMessageDeletionDelay);
                    await botMsg.DeleteAsync();
                    // we return here because we only need to check for at least one match, doesnt matter if there are more
                    return true;
                }                     
            }
            return false;

        }
        public async Task<bool> CheckInviteLinks(SocketMessage msg)
        {
            if (msg.Content.Contains("discord.gg") || msg.Content.Contains("d.gg"))
            {
                // add a bad boye point for the user
                if (userPunishmentCount.ContainsKey(msg.Author.Id))
                {
                    userPunishmentCount[msg.Author.Id] += 1;
                }
                else // they were a good boye but now they are not
                {
                    userPunishmentCount.Add(msg.Author.Id, 1);
                }
                // we run an async task to remove their point after the specified duration
                UserPunishmentTimeout(msg.Author.Id);
                var botMsg = await msg.Channel.SendMessageAsync(msg.Author.Mention + " no invite links!");
                await Task.Delay(botMessageDeletionDelay);
                await botMsg.DeleteAsync();
                // we return here because we only need to check for at least one match, doesnt matter if there are more
                return true;
            }
            return false;
        }
        public async Task<bool> CheckEmojiSpam(SocketMessage msg)
        {
            // check for repeated custom and normal emojis. 
            // custom emojis have format <:name:id> and normal emojis use unicode emoji
            var regex = "( ?(<:.*:[0-9]*>)|(\u00a9|\u00ae|[\u2000-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]) ?){" + maxNumberEmojis + ",}";
            var matchEmoji = Regex.Match(msg.Content, regex);
            if (matchEmoji.Success) // emoji spam
            {
                // add a bad boye point for the user
                if (userPunishmentCount.ContainsKey(msg.Author.Id))
                {
                    userPunishmentCount[msg.Author.Id] += 1;
                }
                else // they were a good boye but now they are not
                {
                    userPunishmentCount.Add(msg.Author.Id, 1);
                }
                // we run an async task to remove their point after the specified duration
                UserPunishmentTimeout(msg.Author.Id);
                var botMsg = await msg.Channel.SendMessageAsync(msg.Author.Mention + " do not spam emojis!");
                await Task.Delay(botMessageDeletionDelay);
                await botMsg.DeleteAsync();
                return true;
            }
            else
                return false;
        }
        public async Task CheckForExcessiveJoins(IGuild guild)
        {
            var _floofDb = new FloofDataContext();
            var server = guild as SocketGuild;
            if (server == null)
                return;
            var serverConfig = GetServerConfig(guild, _floofDb);
            // raid protection not configured
            if (serverConfig == null)
                return;
            // raid protection disabled
            if (!serverConfig.Enabled)
                return;
            // increment user join count for guild
            if (numberOfJoins.ContainsKey(guild))
            {
                numberOfJoins[guild] += 1;
                if (numberOfJoins[guild] >= maxNumberOfJoins)
                {
                    var modRole = (serverConfig.ModRoleId != null) ? server.GetRole((ulong)serverConfig.ModRoleId) : null;
                    var modChannel = (serverConfig.ModChannelId != null) ? server.GetChannel((ulong)serverConfig.ModChannelId) as ITextChannel : null;
                    await NotifyModerators(modRole, modChannel, "Excessive number of new joins in short time period.");
                    numberOfJoins[guild] = 0;
                    return;
                }
            }
            else // they were a good boye but now they are not
            {
                numberOfJoins.Add(guild, 1);
            }
            userJoinTimeout(guild);

        }
        // return true is message triggered raid protection, false otherwise
        public async Task<bool> CheckMessage(FloofDataContext _floofDb, SocketMessage msg)
        {

            // can return null
            var userMsg = msg as SocketUserMessage;
            if (userMsg == null || msg.Author.IsBot)
                return false;
            // can return null
            var channel = userMsg.Channel as ITextChannel;
            if (channel == null)
                return false;
            // can return null
            var guild = channel.Guild as SocketGuild;
            if (guild == null)
                return false;
            var serverConfig = GetServerConfig(guild, _floofDb);
            // raid protection not configured
            if (serverConfig == null)
                return false;
            // raid protection disabled
            if (!serverConfig.Enabled)
                return false;

            // users with the exceptions role are immune to raid protection
            if (serverConfig.ExceptionRoleId != null)
            {
                // returns null if exception role doe not exist anymore
                var exceptionsRole = guild.GetRole((ulong)serverConfig.ExceptionRoleId); 
                var guildUser = guild.GetUser(userMsg.Author.Id);
                // role must exist and user must exist in server
                if (exceptionsRole != null && guildUser != null)
                {
                    foreach (IRole role in guildUser.Roles)
                        {
                        if (role.Id == exceptionsRole.Id)
                            return false;
                        }
                }
            }

            // get other values from db and get their associated roles and channels
            var mutedRole = (serverConfig.MutedRoleId != null) ? guild.GetRole((ulong)serverConfig.MutedRoleId) : null;
            var modRole = (serverConfig.ModRoleId != null) ? guild.GetRole((ulong)serverConfig.ModRoleId) : null;
            var modChannel = (serverConfig.ModChannelId != null) ? guild.GetChannel((ulong)serverConfig.ModChannelId) as ITextChannel : null;
            var banOffenders = serverConfig.BanOffenders;

            // now we run our checks. If any of them return true, we have a bad boy

            // this will ALWAYS ban users regardless of muted role or not 
            bool spammedMentions = CheckMentions(userMsg, guild, modRole, modChannel).Result;
            // this will check their messages and see if they are spamming
            bool userSpammedMessages = CheckUserMessageCount(userMsg).Result;
            // this checks for spamming letters in a row
            bool userSpammedLetters = CheckLetterSpam(userMsg).Result;
            // this checks for posting invite links
            bool userSpammedInviteLink = CheckInviteLinks(userMsg).Result;
            // check for spammed emojis
            bool userSpammedEmojis = CheckEmojiSpam(userMsg).Result;

            if (spammedMentions)
                return false; // user already banned
            if (userSpammedMessages || userSpammedLetters || userSpammedInviteLink || userSpammedEmojis)
            {
                if (userPunishmentCount.ContainsKey(userMsg.Author.Id))
                {
                    // they have been too much of a bad boye >:(
                    if (userPunishmentCount[msg.Author.Id] > maxNumberOfPunishments)
                    {
                        // remove them from the dictionary
                        userPunishmentCount.Remove(userMsg.Author.Id);
                        // add to the list of punished users
                        punishedUsers.Add(userMsg.Author);
                        punishedUsersTimeout(userMsg.Author);
                        // decide if we need to notify the mods of a potential raid
                        if ((modRole != null) && (modChannel != null) && (punishedUsers.Count >= maxNumberPunishedUsers))
                            await NotifyModerators(modRole, modChannel, "Excessive amount of users punished in short time frame.");
                        // if the muted role is set and we are not banning people
                        if ((mutedRole != null) && (!banOffenders))
                        {
                            var guildUser = guild.GetUser(userMsg.Author.Id);
                            try
                            {
                                await guildUser.AddRoleAsync(mutedRole);
                                await msg.Channel.SendMessageAsync(userMsg.Author.Mention + " you have received too many warnings. You are muted as a result.");
                            }
                            catch (Exception e)
                            {
                                Log.Error("Unable to mute user for raid protection: " + e);
                            }
                            return true;
                        }
                        else // ban user by default
                        {
                            try
                            {
                                string reason = "Raid Protection => Triggered too many bot responses";
                                //sends message to user
                                EmbedBuilder builder = new EmbedBuilder();
                                builder.Title = "⚖️ Ban Notification";
                                builder.Description = $"You have been banned from {guild.Name}";
                                builder.AddField("Reason", reason);
                                builder.Color = Discord.Color.Red;
                                await msg.Author.SendMessageAsync("", false, builder.Build());
                                await guild.AddBanAsync(msg.Author, 0, reason);
                            }
                            catch (Exception e)
                            {
                                Log.Error("Unable to ban user for raid protection: " + e);
                            }
                            return false; // dont need to delete message as the ban already handled that
                        }

                    }
                }
                return true;
            }
            return false;
        }
    }
}
