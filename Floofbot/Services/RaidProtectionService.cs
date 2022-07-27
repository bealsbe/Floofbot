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

namespace Floofbot.Services
{
    public class RaidProtectionService
    {
        // Filter service
        WordFilterService _wordFilterService;

        // Load raid config
        // Will store user id and how many counts they've had
        private Dictionary<ulong, Dictionary<ulong, int>> userPunishmentCount = new Dictionary<ulong, Dictionary<ulong, int>>();
        // Used to keep track of the number of messages a user sent for spam protection
        private Dictionary<ulong, Dictionary<ulong, int>> userMessageCount = new Dictionary<ulong, Dictionary<ulong, int>>();
        // Used to track the last message a user has sent in the server
        private Dictionary<ulong, Dictionary<ulong, SocketMessage>> lastUserMessageInGuild = new Dictionary<ulong, Dictionary<ulong, SocketMessage>>();
        // A list of punished users used to detect any potential raids
        private Dictionary<ulong, List<SocketUser>> punishedUsers = new Dictionary<ulong, List<SocketUser>>();
        // Contains the number of joins in a guild in a short time frame
        private Dictionary<IGuild, int> numberOfJoins = new Dictionary<IGuild, int>();

        // these ints hold the raid protection config parameters
        private static int maxMentionCount;
        private static int forgivenDuration; 
        private static int durationForMaxMessages;
        private static int maxNumberOfPunishments;
        private static int botMessageDeletionDelay;
        private static int removePunishedUserDelay; 
        private static int maxNumberPunishedUsers;
        private static int maxNumberOfJoins;
        private static int userJoinsDelay;
        private static int maxNumberEmojis;
        private static int maxNumberSequentialCharacters;
        private static int repeatingPhrasesLimit;
        private static int distanceBetweenPhrases;
        private static int durationBetweenMessages;
        private static int maxMessageSpam;

        public RaidProtectionService()
        {
            var raidConfig = BotConfigFactory.Config.RaidProtection;
            
            _wordFilterService = new WordFilterService();
            
            maxMentionCount = raidConfig["MaxMentionCount"];
            forgivenDuration = raidConfig["ForgivenDuration"];
            durationForMaxMessages = raidConfig["DurationForMaxMessages"];
            maxNumberOfPunishments = raidConfig["MaxNumberOfPunishments"];
            botMessageDeletionDelay = raidConfig["BotMessageDeletionDelay"];
            removePunishedUserDelay = raidConfig["RemovePunishedUserDelay"];
            maxNumberPunishedUsers = raidConfig["MaxNumberPunishedUsers"];
            maxNumberOfJoins = raidConfig["MaxNumberOfJoins"];
            userJoinsDelay = raidConfig["UserJoinsDelay"];
            maxNumberEmojis = raidConfig["MaxNumberEmojis"];
            maxNumberSequentialCharacters = raidConfig["MaxNumberSequentialCharacters"];
            repeatingPhrasesLimit = raidConfig["RepeatingPhrasesLimit"];
            distanceBetweenPhrases = raidConfig["DistanceBetweenPhrases"];
            durationBetweenMessages = raidConfig["DurationBetweenMessages"];
            maxMessageSpam = raidConfig["MaxMessageSpam"];
        }

        private RaidProtectionConfig GetServerConfig(IGuild guild, FloofDataContext _floofDb)
        {
            var serverConfig = _floofDb.RaidProtectionConfigs.Find(guild.Id);
            
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

            if (!userPunishmentCount.ContainsKey(guildId) || !userPunishmentCount[guildId].ContainsKey(userId)) return;
            
            userPunishmentCount[guildId][userId] -= 1;
            
            if (userPunishmentCount[guildId][userId] == 0)
                userPunishmentCount[guildId].Remove(userId);
        }
        
        private async void PunishedUsersTimeout(ulong guildId, SocketUser user)
        {
            await Task.Delay(removePunishedUserDelay);
            
            if (punishedUsers.ContainsKey(guildId) && punishedUsers[guildId].Contains(user))
                punishedUsers[guildId].Remove(user);
        }
        
        private async void UserJoinTimeout(IGuild guild)
        {
            await Task.Delay(userJoinsDelay);

            if (!numberOfJoins.ContainsKey(guild) || numberOfJoins[guild] == 0) return;
            
            numberOfJoins[guild] -= 1;
            
            if (numberOfJoins[guild] == 0)
                numberOfJoins.Remove(guild);
        }
        
        private async void UserMessageCountTimeout(ulong guildId, ulong userId)
        {
            await Task.Delay(durationForMaxMessages);
            if (!userMessageCount.ContainsKey(guildId) || !userMessageCount[guildId].ContainsKey(userId)) return;
            
            if (userMessageCount[guildId][userId] == 0) return;
                
            userMessageCount[guildId][userId] -= 1;
                
            if (userMessageCount[guildId][userId] == 0)
                userMessageCount[guildId].Remove(userId);
        }
        
        private void EnsureGuildInDictionaries(ulong guildId)
        {
            if (!userPunishmentCount.ContainsKey(guildId))
                userPunishmentCount.Add(guildId, new Dictionary<ulong, int>());
            
            if (!userMessageCount.ContainsKey(guildId))
                userMessageCount.Add(guildId, new Dictionary<ulong, int>());
            
            if (!punishedUsers.ContainsKey(guildId))
                punishedUsers.Add(guildId, new List<SocketUser>());
            
            if (!lastUserMessageInGuild.ContainsKey(guildId))
                lastUserMessageInGuild.Add(guildId, new Dictionary<ulong, SocketMessage>());
        }
        
        private bool CheckMessageForFilteredWords(SocketMessage msg, ulong guildId)
        {
            var hasBadWord = _wordFilterService.HasFilteredWord(new FloofDataContext(), msg.Content, guildId, msg.Channel.Id);

            if (!hasBadWord) return false;
            
            // Add a bad boye point for the user
            if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
            {
                userPunishmentCount[guildId][msg.Author.Id] += 1;
            }
            else // They were a good boye but now they are not
            {
                userPunishmentCount[guildId].Add(msg.Author.Id, 1);
            }
                
            // We run an async task to remove their point after the specified duration
            UserPunishmentTimeout(guildId, msg.Author.Id);

            SendMessageAndDelete($"{msg.Author.Mention} There was a filtered word in that message. Please be mindful of your language!", msg.Channel);

            Log.Information("User ID " + msg.Author.Id + " triggered the word filter.");

            // We return here because we only need to check for at least one match, doesnt matter if there are more
            return true;
        }
        
        private bool CheckUserMessageCount(SocketMessage msg, ulong guildId)
        {
            if (userMessageCount[guildId].ContainsKey(msg.Author.Id))
            {
                // Update last user message in server
                if (lastUserMessageInGuild[guildId].ContainsKey(msg.Author.Id))
                {
                    lastUserMessageInGuild[guildId][msg.Author.Id] = msg;
                }
                // Record last user message in server
                else
                {
                    lastUserMessageInGuild[guildId].Add(msg.Author.Id, msg);
                    return false;
                }

                // Compare timestamps of messages
                var timeBetweenMessages = msg.Timestamp - lastUserMessageInGuild[guildId][msg.Author.Id].Timestamp;
                
                if (timeBetweenMessages.TotalSeconds < durationBetweenMessages)
                    userMessageCount[guildId][msg.Author.Id] += 1;
                else
                    return false;

                if (userMessageCount[guildId][msg.Author.Id] >= maxMessageSpam) // No more than a defined number of messages in time frame
                {
                    // Add a bad boye point for the user
                    if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                    {
                        userPunishmentCount[guildId][msg.Author.Id] += 1;
                    }
                    else // They were a good boye but now they are not
                    {
                        userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                    }
                    
                    // Reset the count after punishing the user
                    userMessageCount[guildId][msg.Author.Id] = 0;

                    SendMessageAndDelete(msg.Author.Mention + " you are sending messages too quickly!", msg.Channel);

                    // We run an async task to remove their point after the specified duration
                    UserPunishmentTimeout(guildId, msg.Author.Id);

                    Log.Information("User ID " + msg.Author.Id + " triggered excess message spam and received a warning.");
                    return true;
                }
            }
            else // They were a good boye but now they are not
            {
                userMessageCount[guildId].Add(msg.Author.Id, 1);
            }
            
            // We run an async task to remove their point after the specified duration
            UserMessageCountTimeout(guildId, msg.Author.Id);
            return false;
        }
        
        private async Task<bool> CheckMentions(SocketUserMessage msg, IGuild guild, SocketRole modRole, ITextChannel modChannel)
        {
            if (msg.MentionedUsers.Count <= maxMentionCount) return false;
            
            var reason = "Raid Protection =>" + msg.MentionedUsers.Count + " mentions in one message.";
            
            try
            {
                //sends message to user
                var builder = new EmbedBuilder
                {
                    Title = "⚖️ Ban Notification",
                    Description = $"You have been banned from {guild.Name}",
                    Color = Color.Red
                }.AddField("Reason", reason);
                
                await msg.Author.SendMessageAsync(string.Empty, false, builder.Build());
                
                await guild.AddBanAsync(msg.Author, 0, reason);

                await NotifyModerators(modRole, modChannel, " I have banned a user for mentioning " + msg.MentionedUsers.Count() + " members");
            }
            catch (Exception ex)
            {
                Log.Error("Error banning user for mass mention: " + ex);
            }
            
            Log.Information("User ID " + msg.Author.Id + " triggered excess mention spam and was banned.");
            return true;
        }
        
        private bool CheckLetterSpam(SocketMessage msg, ulong guildId)
        {
            var isMatch = Regex.IsMatch(msg.Content.ToLower(), @"((.)\2{" + maxNumberSequentialCharacters + @",})|((\w\S+)(?=((.{0," + (distanceBetweenPhrases - 1) + @"}|\s*)\4){" + (repeatingPhrasesLimit - 1) + @"}))");
            if (!isMatch) return false;
            
            // Add a bad boye point for the user
            if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
            {
                userPunishmentCount[guildId][msg.Author.Id] += 1;
            }
            else // They were a good boye but now they are not
            {
                userPunishmentCount[guildId].Add(msg.Author.Id, 1);
            }
            
            // We run an async task to remove their point after the specified duration
            UserPunishmentTimeout(guildId, msg.Author.Id);

            SendMessageAndDelete(msg.Author.Mention + " no spamming!", msg.Channel);

            Log.Information("User ID " + msg.Author.Id + " triggered excess letter/phrase spam and received a warning.");

            // We return here because we only need to check for at least one match, doesnt matter if there are more
            return true;
        }
        
        private bool CheckInviteLinks(SocketMessage msg, ulong guildId)
        {
            var regex = "(https?:\\/\\/)?(www\\.)?((discord(app)?\\.com\\/invite)|(discord\\.(gg|io|me|li)))\\/+\\w{2,}\\/?";
            
            if (!Regex.IsMatch(msg.Content, regex)) return false;
            
            // Add a bad boye point for the user
            if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
            {
                userPunishmentCount[guildId][msg.Author.Id] += 1;
            }
            else // They were a good boye but now they are not
            {
                userPunishmentCount[guildId].Add(msg.Author.Id, 1);
            }
            
            // We run an async task to remove their point after the specified duration
            UserPunishmentTimeout(guildId, msg.Author.Id);

            SendMessageAndDelete(msg.Author.Mention + " no invite links!", msg.Channel);

            Log.Information("User ID " + msg.Author.Id + " triggered invite link spam and received a warning.");

            // We return here because we only need to check for at least one match, doesnt matter if there are more
            return true;
        }
        
        private bool CheckEmojiSpam(SocketMessage msg, ulong guildId)
        {
            // Check for repeated custom and normal emojis. 
            // Custom emojis have format <:name:id> and normal emojis use unicode emoji
            var regex = "( ?(<a?:[\\w\\d]+:\\d*>)|(\u00a9|\u00ae|[\u2000-\u200c]|[\u200e-\u3300]|\ud83c[\ud000-\udfff]|\ud83d[\ud000-\udfff]|\ud83e[\ud000-\udfff]) ?){" + maxNumberEmojis + ",}";
            var matchEmoji = Regex.Match(msg.Content, regex);
            
            if (matchEmoji.Success) // Emoji spam
            {
                // Add a bad boye point for the user
                if (userPunishmentCount[guildId].ContainsKey(msg.Author.Id))
                {
                    userPunishmentCount[guildId][msg.Author.Id] += 1;
                }
                else // They were a good boy, but now they are not anymore
                {
                    userPunishmentCount[guildId].Add(msg.Author.Id, 1);
                }
                
                // We run an async task to remove their point after the specified duration
                UserPunishmentTimeout(guildId, msg.Author.Id);

                Log.Information("User ID " + msg.Author.Id + " triggered emoji spam and received a warning.");

                SendMessageAndDelete(msg.Author.Mention + " do not spam emojis!", msg.Channel);

                return true;
            }

            return false;
        }
        
        public async Task CheckForExcessiveJoins(IGuild guild)
        {
            var server = guild as SocketGuild;
            
            if (server == null)
                return;

            await using (var floofDb = new FloofDataContext())
            {
                var serverConfig = GetServerConfig(guild, floofDb);
            
                // Raid protection not configured or disabled
                if (serverConfig == null || !serverConfig.Enabled)
                    return;
            
                // Increment user join count for guild
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
                
                UserJoinTimeout(guild);
            }
        }
        
        // return true is message triggered raid protection, false otherwise
        public async Task<bool> CheckMessage(FloofDataContext _floofDb, SocketMessage msg)
        {
            // Can return null
            var userMsg = msg as SocketUserMessage;
            
            if (userMsg == null || msg.Author.IsBot)
                return false;
            
            // can return null
            var channel = userMsg.Channel as ITextChannel;
            
            if (channel == null)
                return false;
            
            var guild = channel.Guild as SocketGuild;
            
            if (guild == null)
                return false;

            var serverConfig = GetServerConfig(guild, _floofDb);
            
            // Raid protection not configured or disabled
            if (serverConfig == null || !serverConfig.Enabled)
                return false;

            // Users with the exceptions role are immune to raid protection
            if (serverConfig.ExceptionRoleId != null)
            {
                // Returns null if exception role doe not exist anymore
                var exceptionsRole = guild.GetRole((ulong)serverConfig.ExceptionRoleId); 
                var guildUser = guild.GetUser(userMsg.Author.Id);
                
                // Role must exist and user must exist in server
                if (exceptionsRole != null && guildUser != null)
                {
                    foreach (IRole role in guildUser.Roles)
                    {
                        if (role.Id == exceptionsRole.Id)
                            return false;
                    }
                }
            }

            // Get other values from db and get their associated roles and channels
            var mutedRole = (serverConfig.MutedRoleId != null) ? guild.GetRole((ulong)serverConfig.MutedRoleId) : null;
            var modRole = (serverConfig.ModRoleId != null) ? guild.GetRole((ulong)serverConfig.ModRoleId) : null;
            var modChannel = (serverConfig.ModChannelId != null) ? guild.GetChannel((ulong)serverConfig.ModChannelId) as ITextChannel : null;
            var banOffenders = serverConfig.BanOffenders;

            // Ensure our dictionaries contain the guild
            EnsureGuildInDictionaries(guild.Id);
            // Now we run our checks. If any of them return true, we have a bad boy

            // Checks for filtered words
            var filteredWord = CheckMessageForFilteredWords(userMsg, guild.Id);
            // This will ALWAYS ban users regardless of muted role or not 
            var spammedMentions = CheckMentions(userMsg, guild, modRole, modChannel).Result;
            // This will check their messages and see if they are spamming
            var userSpammedMessages = CheckUserMessageCount(userMsg, guild.Id);
            // This checks for spamming letters in a row
            var userSpammedLetters = CheckLetterSpam(userMsg, guild.Id);
            // This checks for posting invite links
            var userSpammedInviteLink = CheckInviteLinks(userMsg, guild.Id);
            // Check for spammed emojis
            var userSpammedEmojis = CheckEmojiSpam(userMsg, guild.Id);

            if (spammedMentions)
                return false; // User already banned

            if (!filteredWord && !userSpammedMessages && !userSpammedLetters && !userSpammedInviteLink && !userSpammedEmojis) return false;

            if (!userPunishmentCount[guild.Id].ContainsKey(userMsg.Author.Id)) return true;

            // They have been too much of a bad boy >:(
            if (userPunishmentCount[guild.Id][msg.Author.Id] <= maxNumberOfPunishments) return true;
            
            // Remove them from the dictionary
            userPunishmentCount[guild.Id].Remove(userMsg.Author.Id);
            
            // Add to the list of punished users
            punishedUsers[guild.Id].Add(userMsg.Author);
            PunishedUsersTimeout(guild.Id, userMsg.Author);
            // Decide if we need to notify the mods of a potential raid
            
            if ((modRole != null) && (modChannel != null) && (punishedUsers.Count >= maxNumberPunishedUsers))
                await NotifyModerators(modRole, modChannel,
                    "Excessive amount of users punished in short time frame.");
            
            // If the muted role is set and we are not banning people
            if ((mutedRole != null) && (!banOffenders))
            {
                var guildUser = guild.GetUser(userMsg.Author.Id);
                
                try
                {
                    await guildUser.AddRoleAsync(mutedRole);
                    await msg.Channel.SendMessageAsync(userMsg.Author.Mention +
                                                       " you have received too many warnings. You are muted as a result.");

                    if (modChannel != null)
                    {
                        var embed = new EmbedBuilder();

                        embed.WithTitle($"🔇 User Muted | {userMsg.Author.Username}#{userMsg.Author.Discriminator}")
                            .WithColor(Color.Teal)
                            .WithDescription(
                                $"{userMsg.Author.Mention} | ``{userMsg.Author.Id}`` has been automatically muted by the raid protection system.")
                            .WithCurrentTimestamp();

                        if (Uri.IsWellFormedUriString(userMsg.Author.GetAvatarUrl(), UriKind.Absolute))
                            embed.WithThumbnailUrl(userMsg.Author.GetAvatarUrl());

                        await modChannel.SendMessageAsync(string.Empty, false, embed.Build());
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Unable to mute user for raid protection: " + ex);
                }

                return true;
            }

            try
            {
                var reason = "Raid Protection => Triggered too many bot responses";
                //sends message to user
                var builder = new EmbedBuilder
                {
                    Title = "⚖️ Ban Notification",
                    Description = $"You have been banned from {guild.Name}",
                    Color = Color.Red
                }.AddField("Reason", reason);
                
                await msg.Author.SendMessageAsync(string.Empty, false, builder.Build());
                await guild.AddBanAsync(msg.Author, 0, reason);
            }
            catch (Exception ex)
            {
                Log.Error("Unable to ban user for raid protection: " + ex);
            }

            return false; // dont need to delete message as the ban already handled that
        }
    }
}
