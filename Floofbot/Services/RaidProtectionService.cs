using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using System;
using System.Collections;
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
        private Dictionary<ulong, Dictionary<ulong, int>> userPunishmentCount = new Dictionary<ulong, Dictionary<ulong, int>>();
        // used to keep track of the number of messages a user sent for spam protection
        private Dictionary<ulong, Dictionary<ulong, int>> userMessageCount = new Dictionary<ulong, Dictionary<ulong, int>>();
        // a list of punished users used to detect any potential raids
        private Dictionary<ulong, List<SocketUser>> punishedUsers = new Dictionary<ulong, List<SocketUser>>();
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

        public RaidProtectionConfig GetServerConfig(IGuild guild, FloofDataContext _floofDb)
        {
            RaidProtectionConfig serverConfig = _floofDb.RaidProtectionConfigs.Find(guild.Id);
            return serverConfig;
        }
        private async Task NotifyModerators(SocketRole modRole, ITextChannel modChannel, string message)
        {
            if (modRole == null || modChannel == null || string.IsNullOrEmpty(message))
            {
                Log.Information("Unable to notify moderators of a possible raid with reason: ``" + message + "``");
                return;
            }
            await modChannel.SendMessageAsync(modRole.Mention + " there may be a possible raid! Reason: ``" + message + "``");
        }
        private async void SendMessageAndDelete(string messageContent, ISocketMessageChannel channel)
        {
            var botMsg = await channel.SendMessageAsync(messageContent);
            await Task.Delay(botMessageDeletionDelay);
            await botMsg.DeleteAsync();
        }
        private async void UserPunishmentTimeout(ulong guildId, ulong userId)
        {
            await Task.Delay(forgivenDuration);
            if (userPunishmentCount.ContainsKey(guildId) && userPunishmentCount[guildId].ContainsKey(userId))
            {
                userPunishmentCount[guildId][userId] -= 1;
                if (userPunishmentCount[guildId][userId] == 0)
                    userPunishmentCount[guildId].Remove(userId);
            }
        }
        private async void punishedUsersTimeout(ulong guildId, SocketUser user)
        {
            await Task.Delay(removePunishedUserDelay);
            if (punishedUsers.ContainsKey(guildId) && punishedUsers[guildId].Contains(user))
                punishedUsers[guildId].Remove(user);
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
        private async void UserMessageCountTimeout(ulong guildId, ulong userId)
        {
            await Task.Delay(durationForMaxMessages);
            if (userMessageCount.ContainsKey(guildId) && userMessageCount[guildId].ContainsKey(userId))
                if (userMessageCount[guildId][userId] != 0)
                {
                    userMessageCount[guildId][userId] -= 1;
                    if (userMessageCount[guildId][userId] == 0)
                        userMessageCount[guildId].Remove(userId);

                }

        }
        private void ensureGuildInDictionaries(ulong guildId)
        {
            if (!userPunishmentCount.ContainsKey(guildId))
                userPunishmentCount.Add(guildId, new Dictionary<ulong, int>());
            if (!userMessageCount.ContainsKey(guildId))
                userMessageCount.Add(guildId, new Dictionary<ulong, int>());
            if (!punishedUsers.ContainsKey(guildId))
                punishedUsers.Add(guildId, new List<SocketUser>());
        }
    private bool CheckUserMessageCount(SocketMessage msg, ulong guildId)
        {
            if (userMessageCount[guildId].ContainsKey(msg.Author.Id))
            {
                userMessageCount[guildId][msg.Author.Id] += 1;
                if (userMessageCount[guildId][msg.Author.Id] >= 5) // no more than 5 messages in time frame
                {
                    // add a bad boye point for the user
                    if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                    {
                        userPunishmentCount[guildId][msg.Author.Id] += 1;
                    }
                    else // they were a good boye but now they are not
                    {
                        userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                    }
                    // reset the count after punishing the user
                    userMessageCount[guildId][msg.Author.Id] = 0;

                    SendMessageAndDelete(msg.Author.Mention + " you are sending messages too quickly!", msg.Channel);

                    // we run an async task to remove their point after the specified duration
                    UserPunishmentTimeout(guildId, msg.Author.Id);

                    Log.Information("User ID " + msg.Author.Id + " triggered excess message spam and received a warning.");
                    return true;
                }
            }
            else // they were a good boye but now they are not
            {
                userMessageCount[guildId].Add(msg.Author.Id, 1);
            }
            // we run an async task to remove their point after the specified duration
            UserMessageCountTimeout(guildId, msg.Author.Id);
            return false;
        }
        private async Task<bool> CheckMentions(SocketUserMessage msg, IGuild guild, SocketRole modRole, ITextChannel modChannel)
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
                Log.Information("User ID " + msg.Author.Id + " triggered excess mention spam and was banned.");
                return true;
            }
            return false;
        }
        private bool CheckLetterSpam(SocketMessage msg, ulong guildId)
        {
            var matches = Regex.Matches(msg.Content, @"(.)\1+");
            foreach (Match m in matches)
            {
                // string has more than 5 letters in a row
                if (m.Length > 5)
                {
                    // add a bad boye point for the user
                    if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                    {
                        userPunishmentCount[guildId][msg.Author.Id] += 1;
                    }
                    else // they were a good boye but now they are not
                    {
                        userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                    }
                    // we run an async task to remove their point after the specified duration
                    UserPunishmentTimeout(guildId, msg.Author.Id);

                    SendMessageAndDelete(msg.Author.Mention + " no spamming!", msg.Channel);

                    Log.Information("User ID " + msg.Author.Id + " triggered excess letter spam and received a warning.");

                    // we return here because we only need to check for at least one match, doesnt matter if there are more
                    return true;
                }                     
            }
            return false;

        }
        private bool CheckInviteLinks(SocketMessage msg, ulong guildId)
        {
            var regex = "(https?:\\/\\/)?(www\\.)?((discord(app)?\\.com\\/invite)|(discord\\.(gg|io|me|li)))\\/+\\w{2,}\\/?";
            if (Regex.IsMatch(msg.Content, regex))
            {
                // add a bad boye point for the user
                if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                {
                    userPunishmentCount[guildId][msg.Author.Id] += 1;
                }
                else // they were a good boye but now they are not
                {
                    userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                }
                // we run an async task to remove their point after the specified duration
                UserPunishmentTimeout(guildId, msg.Author.Id);

                SendMessageAndDelete(msg.Author.Mention + " no invite links!", msg.Channel);

                Log.Information("User ID " + msg.Author.Id + " triggered invite link spam and received a warning.");

                // we return here because we only need to check for at least one match, doesnt matter if there are more
                return true;
            }
            return false;
        }
        private bool CheckEmojiSpam(SocketMessage msg, ulong guildId)
        {
            // check for repeated custom and normal emojis. 
            // custom emojis have format <:name:id> and normal emojis use unicode emoji
            var regex = "( ?(<:.*:[0-9]*>)|(\u00a9|\u00ae|[\u2000\u200c]|[\u200e-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]) ?){" + maxNumberEmojis + ",}";
            var matchEmoji = Regex.Match(msg.Content, regex);
            if (matchEmoji.Success) // emoji spam
            {
                // add a bad boye point for the user
                if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                {
                    userPunishmentCount[guildId][msg.Author.Id] += 1;
                }
                else // they were a good boye but now they are not
                {
                    userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                }
                // we run an async task to remove their point after the specified duration
                UserPunishmentTimeout(guildId, msg.Author.Id);

                Log.Information("User ID " + msg.Author.Id + " triggered emoji spam and received a warning.");

                SendMessageAndDelete(msg.Author.Mention + " do not spam emojis!", msg.Channel);

                return true;
            }
            else
                return false;
        }
        public async Task CheckForExcessiveJoins(IGuild guild)
        {
            var server = guild as SocketGuild;
            if (server == null)
                return;
            var _floofDb = new FloofDataContext();
            var serverConfig = GetServerConfig(guild, _floofDb);
            // raid protection not configured or disabled
            if (serverConfig == null || !serverConfig.Enabled)
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
                    Log.Information("An excessive number of joins was detected in server ID " + server.Id);
                    numberOfJoins[guild] = 0;
                    return;
                }
            }
            else // add 1 to number of joins in that guild
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
            var guild = channel.Guild as SocketGuild;

            if (channel == null || guild == null)
                return false;

            var serverConfig = GetServerConfig(guild, _floofDb);
            // raid protection not configured or disabled
            if (serverConfig == null || !serverConfig.Enabled)
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

            // ensure our dictionaries contain the guild
            ensureGuildInDictionaries(guild.Id);
            // now we run our checks. If any of them return true, we have a bad boy

            // this will ALWAYS ban users regardless of muted role or not 
            bool spammedMentions = CheckMentions(userMsg, guild, modRole, modChannel).Result;
            // this will check their messages and see if they are spamming
            bool userSpammedMessages = CheckUserMessageCount(userMsg, guild.Id);
            // this checks for spamming letters in a row
            bool userSpammedLetters = CheckLetterSpam(userMsg, guild.Id);
            // this checks for posting invite links
            bool userSpammedInviteLink = CheckInviteLinks(userMsg, guild.Id);
            // check for spammed emojis
            bool userSpammedEmojis = CheckEmojiSpam(userMsg, guild.Id);

            if (spammedMentions)
                return false; // user already banned
            if (userSpammedMessages || userSpammedLetters || userSpammedInviteLink || userSpammedEmojis)
            {
                if (userPunishmentCount[guild.Id].ContainsKey(userMsg.Author.Id))
                {
                    // they have been too much of a bad boye >:(
                    if (userPunishmentCount[guild.Id][msg.Author.Id] > maxNumberOfPunishments)
                    {
                        // remove them from the dictionary
                        userPunishmentCount[guild.Id].Remove(userMsg.Author.Id);
                        // add to the list of punished users
                        punishedUsers[guild.Id].Add(userMsg.Author);
                        punishedUsersTimeout(guild.Id, userMsg.Author);
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
