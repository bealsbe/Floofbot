using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Discord.Addons.Interactive;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Discord.Net;

namespace Floofbot.Modules
{
    [Summary("Administration commands")]
    [Name("Administration")]
    public class Administration : InteractiveBase
    {
        private static readonly Color ADMIN_COLOR = Color.DarkOrange;
        private static readonly int MESSAGES_TO_SCAN_PER_CHANNEL_ON_PURGE = 100;
        private FloofDataContext _floofDb;

        public Administration(FloofDataContext floofDb) => _floofDb = floofDb;

        [Command("ban")]
        [Alias("b")]
        [Summary("Bans a user from the server, with an optional reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task YeetUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{16,}"))
                {
                    var userId = Regex.Match(user, @"\d{16,}").Value;
                    
                    if (_floofDb.BansOnJoin.AsQueryable().Any(u => u.UserID == Convert.ToUInt64(userId))) // user is already going to be banned when they join
                    {
                        await Context.Channel.SendMessageAsync("⚠️ Cannot find user - they are already going to be banned when they join!");
                        
                        return;
                    }

                    _floofDb.Add(new BanOnJoin
                    {
                        UserID = Convert.ToUInt64(userId),
                        ModID = Context.Message.Author.Id,
                        ModUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}",
                        Reason = reason
                    });
                    
                    await _floofDb.SaveChangesAsync();
                    
                    await Context.Channel.SendMessageAsync("⚠️ Could not find user, they will be banned next time they join the server!");
                   
                    return;
                }

                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
            }

            try
            {
                // Sends message to user
                var builder = new EmbedBuilder
                {
                    Title = "⚖️ Ban Notification",
                    Description = $"You have been banned from {Context.Guild.Name}",
                    Color = ADMIN_COLOR
                }.AddField("Reason", reason);

                await badUser.SendMessageAsync("", false, builder.Build());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their ban!");
            }

            // Bans the user
            if (badUser != null)
            {
                await Context.Guild.AddBanAsync(badUser.Id, 0,
                    $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

                var modEmbedBuilder = new EmbedBuilder
                    {
                        Title = (":shield: User Banned"),
                        Color = ADMIN_COLOR,
                        Description =
                            $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}"
                    }.AddField("User ID", badUser.Id)
                    .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

                await Context.Channel.SendMessageAsync(string.Empty, false, modEmbedBuilder.Build());
            }
            
            await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
        }

        [Command("pruneban")]
        [Alias("pb")]
        [Summary("Bans a user from the server, with an optional reason and prunes their messages")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task PruneBanUser(
            [Summary("user")] string user,
            [Summary("Number Of Days To Prune")] int pruneDays = 0,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{16,}"))
                {
                    var userId = Regex.Match(user, @"\d{16,}").Value;
                    
                    if (_floofDb.BansOnJoin.AsQueryable().Any(u => u.UserID == Convert.ToUInt64(userId))) // User is already going to be banned when they join
                    {
                        await Context.Channel.SendMessageAsync("⚠️ Cannot find user - they are already going to be banned when they join!");
                        return;
                    }

                    _floofDb.Add(new BanOnJoin
                    {
                        UserID = Convert.ToUInt64(userId),
                        ModID = Context.Message.Author.Id,
                        ModUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}",
                        Reason = reason
                    });
                    
                    _floofDb.SaveChanges();
                    
                    await Context.Channel.SendMessageAsync("⚠️ Could not find user, they will be banned next time they join the server!");
                    return;
                }

                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }

            try
            {
                // Sends message to user
                var builder = new EmbedBuilder
                {
                    Title = "⚖️ Ban Notification",
                    Description = $"You have been banned from {Context.Guild.Name}",
                    Color = ADMIN_COLOR
                }.AddField("Reason", reason);
                
                await badUser.SendMessageAsync("", false, builder.Build());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their ban!");
            }

            // Bans the user
            await Context.Guild.AddBanAsync(badUser.Id, pruneDays, $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

            var modEmbedBuilder = new EmbedBuilder
            {
                Title = (":shield: User Banned"),
                Color = ADMIN_COLOR,
                Description = $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}"
            }.AddField("User ID", badUser.Id)
             .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");
            
            await Context.Channel.SendMessageAsync("", false, modEmbedBuilder.Build());
        }

        [Command("viewautobans")]
        [Summary("View a list of the User IDs that will be autobanned when they join the server")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task ViewAutoBans()
        {
            if (!_floofDb.BansOnJoin.AsQueryable().Any()) // there are no auto bans!
            {
                await Context.Channel.SendMessageAsync("There are no auto bans configured!");
                return;
            }
            
            var autoBans = _floofDb.BansOnJoin.AsQueryable().ToList();
            var pages = new List<PaginatedMessage.Page>();
            var numPages = (int) Math.Ceiling((double) autoBans.Count / 20);

            for (int i = 0; i < numPages; i++)
            {
                var text = "```\n";
                
                for (int j = 0; j < 20; j++)
                {
                    var index = (i * 20) + j;
                    
                    if (index < autoBans.Count)
                    {
                        var modUser = ResolveUser(autoBans[index].ModID.ToString()); // try to resolve the mod who added it
                        var modUsername = ((modUser != null) ? $"{modUser.Username}#{modUser.Discriminator}" : $"{autoBans[index].ModUsername}"); // try to get mod's new username, otherwise, use database stored name
                        text += $"{index + 1}. {autoBans[index].UserID} - added by {modUsername}\n";
                    }
                }
                
                text += "\n```";
                
                pages.Add(new PaginatedMessage.Page
                {
                    Description = text
                });
            }

            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = ADMIN_COLOR,
                Content = Context.User.Mention,
                FooterOverride = null,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };
            
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            });
        }

        [Command("removeautoban")]
        [Summary("Remove a user ID that is configured to be automatically banned")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task RemoveAutoBan([Summary("user ID")] string userId)
        {
            if (!Regex.IsMatch(userId, @"\d{16,}")) // not a valid user ID
            {
                await Context.Channel.SendMessageAsync("This is not a valid user ID! Please specify the User ID you wish to remove from the auto ban list!");
                return;
            }
            
            var user = _floofDb.BansOnJoin.AsQueryable().Where(u => u.UserID == Convert.ToUInt64(userId)).FirstOrDefault();
            
            if (user == null) // there are no auto bans!
            {
                await Context.Channel.SendMessageAsync("This user is not in the auto ban list!");
                return;
            }
            
            try
            {
                _floofDb.Remove(user);
                
                await _floofDb.SaveChangesAsync();
                await Context.Channel.SendMessageAsync($"{userId} will no longer be automatically banned when they join the server!");
            }
            catch (DbUpdateException) // db error
            {
                await Context.Channel.SendMessageAsync($"Unable to remove {userId} from the database.");
            }
        }

        [Command("kick")]
        [Alias("k")]
        [Summary("Kicks a user from the server, with an optional reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task KickUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }
            
            try
            {
                //sends message to user
                EmbedBuilder builder = new EmbedBuilder();
                builder.Title = "🥾 Kick Notification";
                builder.Description = $"You have been Kicked from {Context.Guild.Name}";
                builder.AddField("Reason", reason);
                builder.Color = ADMIN_COLOR;
                await badUser.SendMessageAsync("", false, builder.Build());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their kick!");
            }

            //kicks users
            await Context.Guild.GetUser(badUser.Id).KickAsync(reason);
            var kickBuilder = new EmbedBuilder
            {
                Title = ("🥾 User Kicked"),
                Color = ADMIN_COLOR,
                Description = $"{badUser.Username}#{badUser.Discriminator} has been kicked from {Context.Guild.Name}"
            }.AddField("User ID", badUser.Id)
             .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");
            
            await Context.Channel.SendMessageAsync("", false, kickBuilder.Build());
        }

        [Command("silentkick")]
        [Alias("sk")]
        [Summary("Kicks a user from the server. Does not notify the user.")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task SilentKickUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null)
            {
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }

            // Kicks users
            await Context.Guild.GetUser(badUser.Id).KickAsync(reason);
            var kickBuilder = new EmbedBuilder
            {
                Title = ("🥾 User Silently Kicked"),
                Color = ADMIN_COLOR,
                Description = $"{badUser.Username}#{badUser.Discriminator} has been silently kicked from {Context.Guild.Name}"
            }.AddField("User ID", badUser.Id)
             .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");
            
            await Context.Channel.SendMessageAsync("", false, kickBuilder.Build());
        }

        [Command("warn")]
        [Alias("w")]
        [Summary("Warns a user on the server, with a given reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WarnUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "")
        {
            EmbedBuilder builder;
            
            if (string.IsNullOrEmpty(reason)) {
                builder = new EmbedBuilder() {
                    Description = $"Usage: `warn [user] [reason]`",
                    Color = Color.Magenta
                };
                
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                
                return;
            }

            if(reason.Length > 500) {
                await Context.Channel.SendMessageAsync("Warnings can not exceed 500 characters");
                
                return;
            }

            var badUser = ResolveUser(user);
            ulong uid; // used if no resolved user
            
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{16,}"))
                {
                    uid = Convert.ToUInt64(user);
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                    
                    return;
                }
            }
            else
            {
                uid = badUser.Id;
            }

            _floofDb.Add(new Warning
            {
                DateAdded = DateTime.Now,
                Forgiven = false,
                GuildId = Context.Guild.Id,
                Moderator = $"{Context.User.Username}#{Context.User.Discriminator}",
                ModeratorId = Context.User.Id,
                Reason = reason,
                UserId = uid,
                warningUrl = Context.Message.GetJumpUrl()
            }) ;
            
            await _floofDb.SaveChangesAsync();

            if (badUser != null) // only send if resolved user
            {
                try
                {
                    // Sends message to user
                    
                    builder = new EmbedBuilder
                    {
                        Title = "⚖️ Warn Notification",
                        Description = $"You have recieved a warning in {Context.Guild.Name}",
                        Color = ADMIN_COLOR
                    }.AddField("Reason", reason);
                    
                    await badUser.SendMessageAsync("", false, builder.Build());
                }
                catch (HttpException)
                {
                    await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their warning!");
                }
            }

            builder = new EmbedBuilder
            {
                Title = (":shield: User Warned"),
                Color = ADMIN_COLOR
            }.AddField("User ID", uid)
             .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("usernote")]
        [Alias("un")]
        [Summary("Add a moderation-style user note, give a specified reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UserNote(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "")
        {
            EmbedBuilder builder;
            
            if (string.IsNullOrEmpty(reason)) {
                builder = new EmbedBuilder {
                    Description = "Usage: `usernote [user] [reason]`",
                    Color = Color.Magenta
                };
                
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                
                return;
            }

            if(reason.Length > 500) {
                await Context.Channel.SendMessageAsync("User notes can not exceed 500 characters");
                
                return;
            }

            var badUser = ResolveUser(user);
            ulong uid; // used if no resolved user
            
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{16,}"))
                {
                    uid = Convert.ToUInt64(user);
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                    return;
                }
            }
            else
            {
                uid = badUser.Id;
            }

            _floofDb.Add(new UserNote {
                DateAdded = DateTime.Now,
                Forgiven = false,
                GuildId = Context.Guild.Id,
                Moderator =  $"{Context.User.Username}#{Context.User.Discriminator}",
                ModeratorId = Context.User.Id,
                Reason = reason,
                UserId = uid
            });
            
            await _floofDb.SaveChangesAsync();

            builder = new EmbedBuilder
            {
                Title = (":pencil: User Note Added"),
                Color = ADMIN_COLOR
            }.AddField("User ID", uid)
             .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
        }

        [Command("purge")]
        [Alias("p")]
        [Summary("Deletes recent messages from a given user for all channels on the server")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task PurgeUserMessages(
            [Summary("user")] string user)
        {
            string userId;
            var badUser = ResolveUser(user);
            
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{16,}"))
                {
                    userId = Regex.Match(user, @"\d{16,}").Value;
                }
                else 
                {
                    await Context.Channel.SendMessageAsync("⚠️ Cannot find user");
                    return;
                }
            }
            else
            {
                userId = badUser.Id.ToString();
            }

            // Retrieve user messages from ALL channels
            foreach (ISocketMessageChannel channel in Context.Guild.TextChannels)
            {
                var asyncMessageCollections = channel.GetMessagesAsync(MESSAGES_TO_SCAN_PER_CHANNEL_ON_PURGE);
                
                await foreach(var messageCollection in asyncMessageCollections)
                {
                    foreach (var message in messageCollection)
                    {
                        if (message.Author.Id.ToString() == userId)
                        {
                            await channel.DeleteMessageAsync(message);
                            await Task.Delay(100); // helps reduce the risk of getting rate limited by the API
                        }
                    }
                }
            }

            if (badUser != null)
            {
                var builder = new EmbedBuilder
                    {
                        Title = (":shield: Messages Purged"),
                        Color = ADMIN_COLOR
                    }.AddField("User ID", badUser.Id)
                    .AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

                await Context.Channel.SendMessageAsync("", false, builder.Build());

                return;
            }
            
            await Context.Channel.SendMessageAsync("⚠️ Cannot find user");
        }

        [Command("warnlog")]
        [Alias("wl")]
        [Summary("Displays the warning log for a given user")]
        [RequireContext(ContextType.Guild)]
        public async Task Warnlog([Summary("user")] string user = "")
        {
            var selfUser = Context.Guild.GetUser(Context.Message.Author.Id); // Get the guild user
            Embed embed;
            
            if (string.IsNullOrEmpty(user)) // Want to view their own warnlog 
            {
                    embed = GetWarnings(Context.Message.Author.Id, true);
                    if (embed == null)
                        return;

                    await Context.Message.Author.SendMessageAsync("", false, embed);

                    return;
            }

            if (selfUser.GuildPermissions.KickMembers) // Want to view their own warnlog 
            {
                var badUser = ResolveUser(user);
                
                if (badUser == null)
                {
                    if (UInt64.TryParse(user, out ulong userid))
                    {
                        embed = GetWarnings(userid, false);
                    }
                    else
                    {
                        await Context.Channel.SendMessageAsync("⚠️ Unable to find that user.");
                        
                        return;
                    }
                }
                else
                {
                    embed = GetWarnings(badUser.Id, false);
                }

                if (embed == null)
                    return;
                
                await Context.Channel.SendMessageAsync("", false, embed);
                
                return;
            }

            // Mod wants to view another users log
            await Context.Channel.SendMessageAsync("Only moderators can view the warn logs of other users.");
        }

        [Command("forgive", RunMode = RunMode.Async)]
        [Summary("Remove a user's warning or user notes")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task ForgiveUser([Summary("warning/usernote")] string type = "", [Summary("user")] string badUser = "")
        {
            await UpdateForgivenStatus("forgiven", type, badUser);
        }
        
        [Command("unforgive", RunMode = RunMode.Async)]
        [Summary("Unforgive a warning or user notes")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnforgiveUser([Summary("warning/usernote")] string type = "", [Summary("user")] string badUser = "")
        {
            await UpdateForgivenStatus("unforgiven", type, badUser);
        }

        [Command("mute")]
        [Summary("Applies a mute role to a user")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task MuteUser([Summary("user")]string user, [Summary("Time")]string time = null)
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            IRole muteRole;

            // Check to see if the server exists within the "AdminConfig" Table
            if (!_floofDb.AdminConfig.AsQueryable().Any(x => x.ServerId == Context.Guild.Id)) {
                // Create new mute role
                muteRole = await CreateMuteRole();

                // Save the newly created role
                _floofDb.Add(new AdminConfig {
                    ServerId = Context.Guild.Id,
                    MuteRoleId = muteRole.Id
                });
                
                await _floofDb.SaveChangesAsync();
            }
            else {
                // Grabs the mute role from the database
                muteRole = Context.Guild.GetRole(
                   _floofDb.AdminConfig.AsQueryable()
                   .Where(x => x.ServerId == Context.Guild.Id)
                   .Select(x => x.MuteRoleId).ToList()[0]);

                // Mute role was deleted create a new one
                if (muteRole == null) {
                    muteRole = await CreateMuteRole();
                    
                    var result = _floofDb.AdminConfig
                        .AsQueryable()
                        .SingleOrDefault(x => x.ServerId == Context.Guild.Id);

                    if (result != null) 
                        result.MuteRoleId = muteRole.Id;

                    await _floofDb.SaveChangesAsync();
                }
            }

            if (Context.Guild.GetUser(badUser.Id).Roles.Contains(muteRole)) {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is already muted!");
                
                return;
            }

            await Context.Guild.GetUser(badUser.Id).AddRoleAsync(muteRole);

            var builder = new EmbedBuilder() {
                Title = "🔇 User Muted",
                Description = $"{badUser.Username}#{badUser.Discriminator} Muted!",
                Color = ADMIN_COLOR
            };

            string durationNotifyString = null;
            
            if (time != null) {
                var m = Regex.Match(time, @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?$", 
                    RegexOptions.ExplicitCapture 
                    | RegexOptions.Compiled 
                    | RegexOptions.CultureInvariant 
                    | RegexOptions.RightToLeft);

                var dd = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
                var hs = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
                var ms = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
                var ss = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;

                var seconds = (dd * 86400) + (hs * 60 * 60) + (ms * 60) + ss;

                if (seconds > 0) {
                    var duration = TimeSpan.FromSeconds(seconds);

                    var delayString = String.Empty;

                    if (duration.Days > 0)
                        delayString += $"Days: {duration.Days} ";
                    if (duration.Hours > 0)
                        delayString += $"Hours: {duration.Hours} ";
                    if (duration.Minutes > 0)
                        delayString += $"Minutes: {duration.Minutes} ";
                    if (duration.Seconds > 0)
                        delayString += $"Seconds: {duration.Seconds} ";

                    durationNotifyString = delayString;
                    builder.AddField("Duration", delayString);
                    
                    // Unmute user after duration has expired
                    await Task.Run(async () =>
                    {
                        await Task.Delay(duration);

                        if (Context.Guild.GetUser(badUser.Id).Roles.Contains(muteRole)) {
                            await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(muteRole);
                            
                            try
                            {
                                //notify user that they were unmuted
                                builder = new EmbedBuilder
                                {
                                    Title = "🔊  Unmute Notification",
                                    Description = $"Your Mute on {Context.Guild.Name} has expired",
                                    Color = ADMIN_COLOR
                                };
                                
                                await badUser.SendMessageAsync("", false, builder.Build());
                            }
                            catch (HttpException)
                            {
                                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their unmute!");
                            }
                        }
                    });
                }
                else 
                {
                    await Context.Channel.SendMessageAsync("Invalid Time format... \nExamples: `.mute Talon#6237 1d` `.mute Talon#6237 6h30m`");
                    
                    return;
                }
            }
            
            await Context.Channel.SendMessageAsync("", false, builder.Build());
            
            try
            {
                // Notify user that they were muted
                builder = new EmbedBuilder
                {
                    Title = "🔇  Mute Notification",
                    Description = $"You have been muted on {Context.Guild.Name}"
                };

                if (durationNotifyString != null)
                    builder.AddField("Duration", durationNotifyString);

                builder.Color = ADMIN_COLOR;
                
                await badUser.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their mute!");
            }
        }

        private async Task<IRole> CreateMuteRole()
        {
            var muteRole = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(), Color.DarkerGrey, false, false);

            // Add channel overrides for the new mute role
            foreach (IGuildChannel channel in Context.Guild.Channels) {
                OverwritePermissions permissions = new OverwritePermissions(
                    sendMessages: PermValue.Deny,
                    addReactions: PermValue.Deny,
                    speak: PermValue.Deny
                );

                await channel.AddPermissionOverwriteAsync(muteRole, permissions);
            }

            return muteRole;
        }

        [Command("unmute")]
        [Summary("Removes a mute role from a user")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task UnmuteUser([Summary("user")]string user)
        {
            var badUser = ResolveUser(user);
            
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                
                return;
            }

            var muteRole = Context.Guild.GetRole(
                   _floofDb.AdminConfig
                       .AsQueryable()
                       .Where(x => x.ServerId == Context.Guild.Id)
                       .Select(x => x.MuteRoleId).ToList()[0]);

            if (muteRole == null) {
                await Context.Channel.SendMessageAsync("The Mute Role for this Server Doesn't Exist!\n" +
                    "A new one will be created next time you run the `mute` command");
                
                return;
            }

            if (Context.Guild.GetUser(badUser.Id).Roles.Contains(muteRole)) {
                await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(muteRole);
            }
            else {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is not muted");
                
                return;
            }

            var builder = new EmbedBuilder() {
                Title = "🔊 User Unmuted",
                Description = $"{badUser.Username}#{badUser.Discriminator} was unmuted!",
                Color = ADMIN_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
            
            try
            {
                // Notify user that they were unmuted
                builder = new EmbedBuilder
                {
                    Title = "🔊  Unmute Notification",
                    Description = $"Your Mute on {Context.Guild.Name} has expired",
                    Color = ADMIN_COLOR
                };
                
                await badUser.SendMessageAsync("", false, builder.Build());
            }
            catch (HttpException)
            {
                await Context.Channel.SendMessageAsync("⚠️ | Unable to DM user to notify them of their unmute!");
            }
        }

        [Command("lock")]
        [Summary("Locks a channel")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ChannelLock()
        {
            try {
                var textChannel = (IGuildChannel)Context.Channel;
                var builder = new EmbedBuilder {
                    Description = $"🔒  <#{textChannel.Id}> Locked",
                    Color = Color.Orange,
                };
                
                foreach (IRole role in Context.Guild.Roles.Where(r => !r.Permissions.ManageMessages)) {
                    var perms = textChannel.GetPermissionOverwrite(role).GetValueOrDefault();

                    await textChannel.AddPermissionOverwriteAsync(role, perms.Modify(sendMessages: PermValue.Deny));
                }
                
                await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch {
                await Context.Channel.SendMessageAsync("Something went wrong!");
            }
        }

        [Command("unlock")]
        [Summary("Unlocks a channel")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ChannelUnLock()
        {
            try {
                var textChannel = (IGuildChannel) Context.Channel;
                var builder = new EmbedBuilder {
                    Description = $"🔓  <#{textChannel.Id}> Unlocked",
                    Color = Color.DarkGreen,
                };
                
                foreach (IRole role in Context.Guild.Roles.Where(r => !r.Permissions.ManageMessages)) {
                    var perms = textChannel.GetPermissionOverwrite(role).GetValueOrDefault();
                    
                    if (role.Name != "nadeko-mute" && role.Name != "Muted")
                        await textChannel.AddPermissionOverwriteAsync(role, perms.Modify(sendMessages: PermValue.Allow));
                }
                
                await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
            }
            catch {
                await Context.Channel.SendMessageAsync("Something went wrong!");
            }
        }
        
        private IUser ResolveUser(string input)
        {
            IUser user = null;
            
            // Resolve userID or @mention
            if (Regex.IsMatch(input, @"\d{16,}")) {
                var userId = Regex.Match(input, @"\d{16,}").Value;
                
                user = Context.Client.GetUser(Convert.ToUInt64(userId));
            }
            // Resolve username#0000
            else if (Regex.IsMatch(input, ".*#[0-9]{4}")) {
                var split = input.Split("#");
                
                user = Context.Client.GetUser(split[0], split[1]);
            }
            
            return user;
        }
        
        private Embed CreateDescriptionEmbed(string description)
        {
            var builder = new EmbedBuilder
            {
                Description = description,
                Color = ADMIN_COLOR
            };
            
            return builder.Build();
        }

        private async Task SendEmbed(Embed embed)
        {
            await Context.Channel.SendMessageAsync("", false, embed);
        }
        
        private async Task UpdateForgivenStatus(string function, string type, string badUser)
        {
            IQueryable warnings = null;
            
            var oldStatus = function != "forgiven"; // If we are forgiving them then their old status must be unforgiven
            
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(badUser) || (!type.ToLower().Equals("warning") && !type.ToLower().Equals("usernote"))) // Invalid parameters
            {
                var embed = CreateDescriptionEmbed($"💾 Usage: `{(function == "forgiven" ? "forgive" : "unforgive")} [warning/usernote] [user]`");
                
                await SendEmbed(embed);
                
                return;
            }
            
            var user = ResolveUser(badUser);

            ulong uId;
            
            if (user != null)
            {
                uId = user.Id;
            }
            else if (Regex.IsMatch(badUser, @"\d{16,}")) // not in server but valid user id
            {
                uId = Convert.ToUInt64(badUser);
            }
            else // user not in server AND not valid user id
            {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{badUser}\"");
                return;
            }

            switch (type)
            {
                // Forgive warning
                // There are no warnings for this user in this guild
                case "warning" when !_floofDb.Warnings.AsQueryable().Any(w => w.UserId == uId && w.GuildId == Context.Guild.Id && w.Forgiven == oldStatus):
                    await Context.Channel.SendMessageAsync($"User has no warnings to be {function}!");
                    return;
                
                case "warning":
                    warnings = _floofDb.Warnings.AsQueryable()
                        .Where(u => u.UserId == uId && u.GuildId == Context.Guild.Id && u.Forgiven == oldStatus)
                        .OrderByDescending(x => x.DateAdded).Take(10);
                    break;
                
                // Forgive usernote
                // There are no user notes for this user in this guild
                case "usernote" when !_floofDb.UserNotes.AsQueryable().Any(w => w.UserId == uId && w.GuildId == Context.Guild.Id && w.Forgiven == oldStatus):
                    await Context.Channel.SendMessageAsync($"User has no user notes to be {function}!");
                    return;
                
                case "usernote":
                    warnings = _floofDb.UserNotes.AsQueryable()
                        .Where(u => u.UserId == uId && u.GuildId == Context.Guild.Id && u.Forgiven == oldStatus)
                        .OrderByDescending(x => x.DateAdded).Take(10);
                    break;
            }

            var builder = new EmbedBuilder
            {
                Color = ADMIN_COLOR
            };

            builder.WithTitle(user == null
                ? $"{((type == "warnings") ? "Warnings" : "User Notes")} for {badUser}"
                : $"{((type == "warnings") ? "Warnings" : "User Notes")} for {user.Username}#{user.Discriminator}");
            
            if (warnings == null) // For some reason didnt recieve data from database
            {
                Log.Error("Fatal error when trying to access warnings for the forgive user command!");
                return;
            }
            
            switch (type)
            {
                case "warning":
                    foreach (Warning w in warnings)
                    {
                        builder.AddField($"**ID: {w.Id}** - {w.DateAdded:yyyy MMMM dd} - {w.Moderator}", $"```{w.Reason}```");
                    }

                    break;
                
                case "usernote":
                    foreach (UserNote w in warnings)
                    {
                        builder.AddField($"**ID: {w.Id}** - {w.DateAdded:yyyy MMMM dd} - {w.Moderator}", $"```{w.Reason}```");
                    }

                    break;
            }
            
            await SendEmbed(builder.Build());
            
            try
            {
                await ReplyAsync($"Which would you like to be {function}? Please specify the ID.");

                var response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10)); // Wait for reply from source user in source channel for 10 seconds
                
                if (response == null)
                {
                    await Context.Channel.SendMessageAsync("You did not respond in time. Aborting...");
                    
                    return;
                }

                if (ulong.TryParse(response.Content, out var warningId)) // response is of type integer
                {
                    if (type == "warning")
                    {
                        foreach (Warning w in warnings)
                        {
                            if (w.Id == warningId)
                            {
                                var modId = Context.Message.Author.Id;
                                var modUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}";
                                
                                await SetWarningForgivenStatus(w, !oldStatus, modId);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has {function} the warning with the ID {w.Id} and the reason: {w.Reason}.");
                                
                                return;
                            }
                        }
                    }
                    else if (type == "usernote")
                    {
                        foreach (UserNote un in warnings)
                        {
                            if (un.Id == warningId)
                            {
                                var modId = Context.Message.Author.Id;
                                var modUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}";
                                
                                await SetUserNoteForgivenStatus(un, !oldStatus, modId);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has {function} the user note with the ID {un.Id} and the reason: {un.Reason}.");
                                
                                return;
                            }
                        }
                    }
                    
                    await Context.Channel.SendMessageAsync("You have provided either an incorrect response, or that warning ID is not in the list of warnings. Aborting...");
                    
                    return;
                }

                await Context.Channel.SendMessageAsync("Invalid input, please provide a valid number. Aborting..");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }
        
        private async Task SetWarningForgivenStatus(Warning warning, bool status, ulong forgivenBy)
        {
            warning.Forgiven = status;
            warning.ForgivenBy = forgivenBy;
            
            await _floofDb.SaveChangesAsync();
        }
        
        private async Task SetUserNoteForgivenStatus(UserNote userNote, bool status, ulong forgivenBy)
        {
            userNote.Forgiven = status;
            userNote.ForgivenBy = forgivenBy;
            
            await _floofDb.SaveChangesAsync();
        }
        
        private Embed GetWarnings(ulong uid, bool isOwnLog)
        {
            IQueryable<Warning> formalWarnings;
            IQueryable<UserNote> userNotes = null;
            
            var badUser = Context.Client.GetUser(uid);

            if (isOwnLog)
            {
                formalWarnings = _floofDb.Warnings.AsQueryable()
                    .Where(u => u.UserId == uid && u.GuildId == Context.Guild.Id && u.Forgiven == false)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }
            else // User notes are for mod view only
            {
                formalWarnings = _floofDb.Warnings.AsQueryable()
                    .Where(u => u.UserId == uid && u.GuildId == Context.Guild.Id)
                    .OrderByDescending(x => x.DateAdded).Take(10);

                userNotes = _floofDb.UserNotes.AsQueryable()
                    .Where(u => u.UserId == uid && u.GuildId == Context.Guild.Id)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }

            if (!isOwnLog) // Mod viewing someones history
            {
                if (badUser == null) // Client cant get user - no mutual servers?
                {
                    if (!formalWarnings.Any() && !userNotes.Any())
                    {
                        var message = $"{uid} is a good noodle. They have no warnings or user notes!";
                        var embed = CreateDescriptionEmbed(message);
                        
                        return embed;
                    }
                }
                else
                {
                    if (!formalWarnings.Any() && !userNotes.Any())
                    {
                        string message = $"{badUser.Username}#{badUser.Discriminator} is a good noodle. They have no warnings or user notes!";
                        var embed = CreateDescriptionEmbed(message);
                        return embed;
                    }
                }
            }
            else // Own users history
            {
                if (!formalWarnings.Any())
                {
                    string message = $"You are a good noodle. You have no warnings!";
                    var embed = CreateDescriptionEmbed(message);
                    return embed;
                }
            }

            var builder = new EmbedBuilder
            {
                Color = ADMIN_COLOR
            };
            
            var warningCount = 0;
            var userNoteCount = 0;

            if (badUser == null && !isOwnLog) // No user, just id in database
                builder.WithTitle($"Warnings for {uid}");
            else if (badUser != null && !isOwnLog)
                builder.WithTitle($"Warnings for {badUser.Username}#{badUser.Discriminator}");
            else
                builder.WithTitle($"Your Warnings");

            if (!isOwnLog)
            {
                if (formalWarnings.Count() != 0) // They have warnings
                {
                    builder.AddField(":warning: | Formal Warnings:", "\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_");
                    
                    foreach (var warning in formalWarnings)
                    {
                        var hyperLink = String.Empty;
                        
                        if (warning.warningUrl != null && Uri.IsWellFormedUriString(warning.warningUrl, UriKind.Absolute)) // make sure url is good
                            hyperLink = $"[Jump To Warning]({warning.warningUrl})\n";

                        if (warning.Forgiven)
                        {
                            var forgivenBy = ResolveUser(warning.ForgivenBy.ToString());
                            var forgivenByText = (forgivenBy == null) 
                                ? string.Empty
                                : $"(forgiven by {forgivenBy.Username}#{forgivenBy.Discriminator})";
                            
                            builder.AddField($"~~**{warningCount + 1}**. {warning.DateAdded:yyyy MMMM dd} - {warning.Moderator}~~ {forgivenByText}", $"{hyperLink}```{warning.Reason}```");
                        }
                        else
                        {
                            builder.AddField($"**{warningCount + 1}**. {warning.DateAdded:yyyy MMMM dd} - {warning.Moderator}", $"{hyperLink}```{warning.Reason}```");
                        }
                        warningCount++;
                    }
                }
            }
            else
            {
                builder.AddField(":warning: | Formal Warnings:", "\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_");
                
                foreach (var warning in formalWarnings)
                {
                    if (warning.Forgiven) // User doesnt need to see forgiven warnings
                    {
                        continue;
                    }

                    builder.AddField($"**{warningCount + 1}**. {warning.DateAdded.ToString("yyyy MMMM dd")}", $"```{warning.Reason}```");
                    warningCount++;
                }
            }

            if (isOwnLog) 
                return builder.Build();

            if (!userNotes.Any()) 
                return builder.Build();
            
            builder.AddField("\u200B", "\u200B"); // blank line
            builder.AddField(":pencil: | User Notes:", "\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_");
            
            foreach (var usernote in userNotes)
            {
                if (usernote.Forgiven)
                {
                    var forgivenBy = ResolveUser(usernote.ForgivenBy.ToString());
                    var forgivenByText = (forgivenBy == null) 
                        ? string.Empty 
                        : $"(forgiven by {forgivenBy.Username}#{forgivenBy.Discriminator})";
                    
                    builder.AddField($"~~**{userNoteCount + 1}**. {usernote.DateAdded.ToString("yyyy MMMM dd")} - {usernote.Moderator}~~ {forgivenByText}", $"```{usernote.Reason}```");
                }
                else
                {
                    builder.AddField($"**{userNoteCount + 1}**. {usernote.DateAdded.ToString("yyyy MMMM dd")} - {usernote.Moderator}", $"```{usernote.Reason}```");
                    userNoteCount++;
                }
            }
            
            return builder.Build();
        }
    }
}
