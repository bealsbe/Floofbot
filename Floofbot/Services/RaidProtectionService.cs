using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using System;
using System.Collections.Generic;
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
        protected Dictionary<ulong, int> userPunishmentCount = new Dictionary<ulong, int>();
        // used to keep track of the number of messages a user sent for spam protection
        protected Dictionary<ulong, int> userMessageCount = new Dictionary<ulong, int>();
        // determines how long before a user is forgiven for a punishment - currently 5 min
        private static int forgivenDuration = 1 * 60 * 1000;
        // determines the rate at which users can send messages, currently no more than x messages in 5 seconds
        private static int durationForMaxMessages = 5 * 1000;
        // determines the max number of punishments a user can have before being punished
        private static int maxNumberOfPunishments = 3;



        public RaidProtectionService()
        {

        }
        public RaidProtectionConfig GetServerConfig(IGuild guild, FloofDataContext _floofDb)
        {
            RaidProtectionConfig serverConfig = _floofDb.RaidProtectionConfigs.Find(guild.Id);
            return serverConfig;
        }
        private async void UserPunishmentTimeout(ulong userid)
        {
            await Task.Delay(forgivenDuration);
            if (userPunishmentCount.ContainsKey(userid))
                if (userPunishmentCount[userid] != 0)
                    userPunishmentCount[userid] -= 1;
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
                    await msg.Channel.SendMessageAsync(msg.Author.Mention + " you are sending messages too quickly!");
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
        public async Task CheckMentions(SocketUserMessage msg, IGuild guild)
        {
            if (msg.MentionedUsers.Count > 10)
            {
                string reason = "Raid Protection => " + msg.MentionedUsers.Count + " mentions in one message.";
                try
                {
                    await guild.AddBanAsync(msg.Author, 1, reason);
                }
                catch (Exception e)
                {
                    Log.Error("Error banning user for mass mention: " + e);
                }
                return;
            }
                
        }
        public async Task<bool> CheckLetterSpam(SocketMessage msg)
        {
            var matches = Regex.Matches(msg.Content, @"(.)\1+");
            foreach (Match m in matches)
            {
                // string has more than 5 letters in a row
                if (m.Length > 5)
                {
                    await msg.Channel.SendMessageAsync(msg.Author.Mention + " no spamming!");
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
                await msg.Channel.SendMessageAsync(msg.Author.Mention + " no invite links!");
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
                // we return here because we only need to check for at least one match, doesnt matter if there are more
                return true;
            }
            return false;
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
                var guildUser = guild.GetUser(msg.Author.Id);
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
            await CheckMentions(userMsg, guild);
            // this will check their messages and see if they are spamming
            bool userSpammedMessages = CheckUserMessageCount(msg).Result;
            // this checks for spamming letters in a row
            bool userSpammedLetters = CheckLetterSpam(msg).Result;
            // this checks for posting invite links
            bool userSpammedInviteLink = CheckInviteLinks(msg).Result;

            if (userSpammedMessages || userSpammedLetters || userSpammedInviteLink)
            {
                if (userPunishmentCount.ContainsKey(msg.Author.Id))
                {
                    // they have been too much of a bad boye >:(
                    if (userPunishmentCount[msg.Author.Id] > maxNumberOfPunishments)
                    {
                        // remove them from the dictionary
                        userPunishmentCount.Remove(msg.Author.Id);
                        // if the muted role is set and we are not banning people
                        if ((mutedRole != null) && (!banOffenders))
                        {
                            var guildUser = guild.GetUser(msg.Author.Id);
                            try
                            {
                                await guildUser.AddRoleAsync(mutedRole);
                                await msg.Channel.SendMessageAsync(msg.Author.Mention + " you have received too many warnings. You are muted as a result.");
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
                                await guild.AddBanAsync(msg.Author, 1, "Raid Protection => Triggered too many bot responses");
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
