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

namespace Floofbot.Modules
{
    [Summary("Administration commands")]
    [Discord.Commands.Name("Administration")]
    public class Administration : InteractiveBase
    {
        private static readonly Color ADMIN_COLOR = Color.DarkOrange;
        private static readonly int MESSAGES_TO_SCAN_PER_CHANNEL_ON_PURGE = 100;
        private FloofDataContext _floofDB;

        public Administration(FloofDataContext floofDB) => _floofDB = floofDB;

        [Command("ban")]
        [Alias("b")]
        [Summary("Bans a user from the server, with an optional reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task YeetUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                if (Regex.IsMatch(user, @"\d{17,18}"))
                {
                    string userID = Regex.Match(user, @"\d{17,18}").Value;
                    if (_floofDB.BansOnJoin.AsQueryable().Any(u => u.UserID == Convert.ToUInt64(userID))) // user is already going to be banned when they join
                    {
                        await Context.Channel.SendMessageAsync("⚠️ Cannot find user - they are already going to be banned when they join!");
                        return;
                    }
                    else
                    {
                        _floofDB.Add(new BanOnJoin
                        {
                            UserID = Convert.ToUInt64(userID),
                            ModID = Context.Message.Author.Id,
                            ModUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}",
                            Reason = reason
                        }) ;
                        _floofDB.SaveChanges();
                        await Context.Channel.SendMessageAsync("⚠️ Could not find user, they will be banned next time they join the server!");
                        return;
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                    return;
                }
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "⚖️ Ban Notification";
            builder.Description = $"You have been banned from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());

            //bans the user
            await Context.Guild.AddBanAsync(badUser.Id, 0, $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Banned");
            builder.Color = ADMIN_COLOR;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("pruneban")]
        [Alias("pb")]
        [Summary("Bans a user from the server, with an optional reason and prunes their messages")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task pruneBanUser(
            [Summary("user")] string user,
            [Summary("Number Of Days To Prune")] int pruneDays = 0,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(user);
            if (badUser == null)
            {
                if (Regex.IsMatch(user, @"\d{17,18}"))
                {
                    string userID = Regex.Match(user, @"\d{17,18}").Value;
                    if (_floofDB.BansOnJoin.AsQueryable().Any(u => u.UserID == Convert.ToUInt64(userID))) // user is already going to be banned when they join
                    {
                        await Context.Channel.SendMessageAsync("⚠️ Cannot find user - they are already going to be banned when they join!");
                        return;
                    }
                    else
                    {
                        _floofDB.Add(new BanOnJoin
                        {
                            UserID = Convert.ToUInt64(userID),
                            ModID = Context.Message.Author.Id,
                            ModUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}",
                            Reason = reason
                        });
                        _floofDB.SaveChanges();
                        await Context.Channel.SendMessageAsync("⚠️ Could not find user, they will be banned next time they join the server!");
                        return;
                    }
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                    return;
                }
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "⚖️ Ban Notification";
            builder.Description = $"You have been banned from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());

            //bans the user
            await Context.Guild.AddBanAsync(badUser.Id, pruneDays, $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Banned");
            builder.Color = ADMIN_COLOR;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("viewautobans")]
        [Summary("View a list of the User IDs that will be autobanned when they join the server")]
        [RequireUserPermission(GuildPermission.BanMembers)]
        [RequireContext(ContextType.Guild)]
        public async Task ViewAutoBans()
        {
            if (!_floofDB.BansOnJoin.AsQueryable().Any()) // there are no auto bans!
            {
                await Context.Channel.SendMessageAsync("There are no auto bans configured!");
                return;
            }
            List<BanOnJoin> autoBans = _floofDB.BansOnJoin.AsQueryable().ToList();

            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();
            int numPages = (int)Math.Ceiling((double)autoBans.Count / 20);
            int index;
            for (int i = 0; i < numPages; i++)
            {
                string text = "```\n";
                for (int j = 0; j < 20; j++)
                {
                    index = i * 20 + j;
                    if (index < autoBans.Count)
                    {
                        var modUser = resolveUser(autoBans[index].ModID.ToString()); // try to resolve the mod who added it
                        var modUsername = ((modUser != null) ? $"{modUser.Username}#{modUser.Discriminator}" : $"{autoBans[index].ModUsername}"); // try to get mod's new username, otherwise, use database stored name
                        text += $"{index + 1}. {autoBans[index].UserID} - added by {modUsername}\n";
                    }
                }
                text += "\n```";
                pages.Add(new PaginatedMessage.Page
                {
                    Description = text
                });
            };

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
            if (!Regex.IsMatch(userId, @"\d{17,18}")) // not a valid user ID
            {
                await Context.Channel.SendMessageAsync("This is not a valid user ID! Please specify the User ID you wish to remove from the auto ban list!");
                return;
            }
            BanOnJoin user = _floofDB.BansOnJoin.AsQueryable().Where(u => u.UserID == Convert.ToUInt64(userId)).FirstOrDefault();
            if (user == null) // there are no auto bans!
            {
                await Context.Channel.SendMessageAsync("This user is not in the auto ban list!");
                return;
            }
            try
            {
                _floofDB.Remove(user);
                await _floofDB.SaveChangesAsync();
                await Context.Channel.SendMessageAsync($"{userId} will no longer be automatically banned when they join the server!");
                return;
            }
            catch (DbUpdateException) // db error
            {
                await Context.Channel.SendMessageAsync($"Unable to remove {userId} from the database.");
                return;
            }
        }

        [Command("kick")]
        [Alias("k")]
        [Summary("Kicks a user from the server, with an optional reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task kickUser(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "🥾 Kick Notification";
            builder.Description = $"You have been Kicked from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());

            //kicks users
            await Context.Guild.GetUser(badUser.Id).KickAsync(reason);
            builder = new EmbedBuilder();
            builder.Title = ("🥾 User Kicked");
            builder.Color = ADMIN_COLOR;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been kicked from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("warn")]
        [Alias("w")]
        [Summary("Warns a user on the server, with a given reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnUser(
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

            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            _floofDB.Add(new Warning {
                DateAdded = DateTime.Now,
                Forgiven = false,
                GuildId = Context.Guild.Id,
                Moderator =  $"{Context.User.Username}#{Context.User.Discriminator}",
                ModeratorId = Context.User.Id,
                Reason = reason,
                UserId = badUser.Id
            });
            _floofDB.SaveChanges();

            //sends message to user
            builder = new EmbedBuilder();
            builder.Title = "⚖️ Warn Notification";
            builder.Description = $"You have recieved a warning in {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Warned");
            builder.Color = ADMIN_COLOR;
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("usernote")]
        [Alias("un")]
        [Summary("Add a moderation-style user note, give a specified reason")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task userNote(
            [Summary("user")] string user,
            [Summary("reason")][Remainder] string reason = "")
        {
            EmbedBuilder builder;
            if (string.IsNullOrEmpty(reason)) {
                builder = new EmbedBuilder() {
                    Description = $"Usage: `usernote [user] [reason]`",
                    Color = Color.Magenta
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                return;
            }

            if(reason.Length > 500) {
                await Context.Channel.SendMessageAsync("User notes can not exceed 500 characters");
                return;
            }

            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            _floofDB.Add(new UserNote {
                DateAdded = DateTime.Now,
                Forgiven = false,
                GuildId = Context.Guild.Id,
                Moderator =  $"{Context.User.Username}#{Context.User.Discriminator}",
                ModeratorId = Context.User.Id,
                Reason = reason,
                UserId = badUser.Id
            });
            _floofDB.SaveChanges();

            builder = new EmbedBuilder();
            builder.Title = (":pencil: User Note Added");
            builder.Color = ADMIN_COLOR;
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("purge")]
        [Alias("p")]
        [Summary("Deletes recent messages from a given user for all channels on the server")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task PurgeUserMessages(
            [Summary("user")] string user)
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            // retrieve user messages from ALL channels
            foreach (ISocketMessageChannel channel in Context.Guild.TextChannels)
            {
                var asyncMessageCollections = channel.GetMessagesAsync(MESSAGES_TO_SCAN_PER_CHANNEL_ON_PURGE);
                await foreach(var messageCollection in asyncMessageCollections)
                {
                    foreach (var message in messageCollection)
                    {
                        if (message.Author.Id == badUser.Id)
                        {
                            await channel.DeleteMessageAsync(message);
                            await Task.Delay(100); // helps reduce the risk of getting rate limited by the API
                        }
                    }
                }
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = (":shield: Messages Purged");
            builder.Color = ADMIN_COLOR;
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("warnlog")]
        [Alias("wl")]
        [Summary("Displays the warning log for a given user")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnlog([Summary("user")] string user)
        {
            IUser badUser = resolveUser(user);
            IQueryable<Warning> formalWarnings;
            IQueryable<UserNote> userNotes;
            if (badUser == null)
            {

                // user not in server, check if user is in database
                if (Regex.IsMatch(user, @"\d{17,18}"))
                {
                    string userID = Regex.Match(user, @"\d{17,18}").Value;
                    formalWarnings = _floofDB.Warnings.AsQueryable()
                                                    .Where(u => u.UserId == Convert.ToUInt64(user) && u.GuildId == Context.Guild.Id)
                                                    .OrderByDescending(x => x.DateAdded).Take(10);
                    userNotes = _floofDB.UserNotes.AsQueryable()
                                                    .Where(u => u.UserId == Convert.ToUInt64(user) && u.GuildId == Context.Guild.Id)
                                                    .OrderByDescending(x => x.DateAdded).Take(10);
                }
                else
                {
                    await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                    return;
                }
            }
            else
            {
                formalWarnings = _floofDB.Warnings.AsQueryable()
                    .Where(u => u.UserId == badUser.Id && u.GuildId == Context.Guild.Id)
                    .OrderByDescending(x => x.DateAdded).Take(10);
                userNotes = _floofDB.UserNotes.AsQueryable()
                    .Where(u => u.UserId == badUser.Id && u.GuildId == Context.Guild.Id)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }

            if (formalWarnings.Count() == 0 && userNotes.Count() == 0)
            {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is a good noodle. They have no warnings or user notes!");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = ADMIN_COLOR;
            int warningCount = 0;
            int userNoteCount = 0;
            if (badUser == null) // no user, just id in database
                builder.WithTitle($"Warnings for {user}");
            else
                builder.WithTitle($"Warnings for {badUser.Username}#{badUser.Discriminator}");
            if (formalWarnings.Count() != 0) // they have warnings
            {
                builder.AddField(":warning: | Formal Warnings:", "\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_");
                foreach (Warning warning in formalWarnings)
                {
                    if (warning.Forgiven)
                    {
                        IUser forgivenBy = resolveUser(warning.ForgivenBy.ToString());
                        var forgivenByText = (forgivenBy == null) ? "" : $"(forgiven by {forgivenBy.Username}#{forgivenBy.Discriminator})";
                        builder.AddField($"~~**{warningCount + 1}**. {warning.DateAdded.ToString("yyyy MMMM dd")} - {warning.Moderator}~~ {forgivenByText}", $"```{warning.Reason}```");
                    }
                    else
                    {
                        builder.AddField($"**{warningCount + 1}**. {warning.DateAdded.ToString("yyyy MMMM dd")} - {warning.Moderator}", $"```{warning.Reason}```");
                    }
                    warningCount++;
                }
            }
            if (userNotes.Count() != 0) // they have user notes
            {
                builder.AddField("\u200B", "\u200B"); // blank line
                builder.AddField(":pencil: | User Notes:", "\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_\\_");
                foreach (UserNote usernote in userNotes)
                {
                    if (usernote.Forgiven)
                    {
                        IUser forgivenBy = resolveUser(usernote.ForgivenBy.ToString());
                        var forgivenByText = (forgivenBy == null) ? "" : $"(forgiven by {forgivenBy.Username}#{forgivenBy.Discriminator})";
                        builder.AddField($"~~**{userNoteCount + 1}**. {usernote.DateAdded.ToString("yyyy MMMM dd")} - {usernote.Moderator}~~ {forgivenByText}", $"```{usernote.Reason}```");

                    }
                    else
                    {
                        builder.AddField($"**{userNoteCount + 1}**. {usernote.DateAdded.ToString("yyyy MMMM dd")} - {usernote.Moderator}", $"```{usernote.Reason}```");
                        userNoteCount++;
                    }
                }
            }
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("forgive", RunMode = RunMode.Async)]
        [Summary("Remove a user's warning or user notes")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task forgiveUser([Summary("warning/usernote")] string type = "", [Summary("user")] string badUser = "")
        {
            IQueryable warnings = null;
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(badUser) || (!type.ToLower().Equals("warning") && !type.ToLower().Equals("usernote"))) // invalid parameters
            {
                Embed embed = CreateDescriptionEmbed($"💾 Usage: `forgive [warning/usernote] [user]`");
                await SendEmbed(embed);
                return;
            }
            IUser user = resolveUser(badUser);

            ulong uID;
            if (user != null)
            {
                uID = user.Id;
            }
            else if (Regex.IsMatch(badUser, @"\d{17,18}")) // not in server but valid user id
            {
                uID = Convert.ToUInt64(badUser);
            }
            else // user not in server AND not valid user id
            {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{badUser}\"");
                return;
            }

            if (type == "warning") // forgive warning
            {
                if (!_floofDB.Warnings.AsQueryable().Where(w => w.UserId == uID && w.GuildId == Context.Guild.Id && w.Forgiven == false).Any()) // there are no warnings for this user in this guild
                {
                    await Context.Channel.SendMessageAsync("User has no warnings to forgive!");
                    return;
                }
                warnings = _floofDB.Warnings.AsQueryable()
                    .Where(u => u.UserId == uID && u.GuildId == Context.Guild.Id && u.Forgiven == false)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }
            else if (type == "usernote") // forgive usernote
            {
                if (!_floofDB.UserNotes.AsQueryable().Where(w => w.UserId == uID && w.GuildId == Context.Guild.Id && w.Forgiven == false).Any()) // there are no user notes for this user in this guild
                {
                    await Context.Channel.SendMessageAsync("User has no warnings to forgive!");
                    return;
                }
                warnings = _floofDB.UserNotes.AsQueryable()
                    .Where(u => u.UserId == uID && u.GuildId == Context.Guild.Id && u.Forgiven == false)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = ADMIN_COLOR;
            if (user == null) // no user, just id in database
                builder.WithTitle($"Warnings for {badUser}");
            else
                builder.WithTitle($"Warnings for {user.Username}#{user.Discriminator}");
            if (warnings == null) // for some reason didnt recieve data from database
            {
                return;
            }
            if (type == "warning")
            {
                foreach (Warning w in warnings)
                {
                    builder.AddField($"**ID: {w.Id}** - {w.DateAdded.ToString("yyyy MMMM dd")} - {w.Moderator}", $"```{w.Reason}```");
                }
            }
            else if (type == "usernote")
            {
                foreach (UserNote w in warnings)
                {
                    builder.AddField($"**ID: {w.Id}** - {w.DateAdded.ToString("yyyy MMMM dd")} - {w.Moderator}", $"```{w.Reason}```");
                }
            }
            await SendEmbed(builder.Build());
            try
            {
                await ReplyAsync("Which would you like to forgive? Please specify the ID.");

                SocketMessage response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10)); // wait for reply from source user in source channel for 2 seconds
                if (response == null)
                {
                    await Context.Channel.SendMessageAsync("You did not respond in time. Aborting...");
                    return;
                }

                ulong warningId;
                if (ulong.TryParse(response.Content, out warningId)) // response is of type integer
                {
                    if (type == "warning")
                    {

                        foreach (Warning w in warnings)
                        {
                            if (w.Id == warningId)
                            {
                                var modId = Context.Message.Author.Id;
                                var modUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}";
                                await SetWarningForgivenStatus(w, true, modId);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has forgiven the warning with the ID {w.Id} and the reason: {w.Reason}.");
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
                                await SetUserNoteForgivenStatus(un, true, modId);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has forgiven the user note with the ID {un.Id} and the reason: {un.Reason}.");
                                return;
                            }
                        }
                    }
                    await Context.Channel.SendMessageAsync("You have provided either an incorrect response, or that warning ID is not in the list of warnings. Aborting...");
                    return;
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Invalid input, please provide a valid number. Aborting..");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }


        [Command("unforgive", RunMode = RunMode.Async)]
        [Summary("Unforgive a warning or user notes")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task unforgiveUser([Summary("warning/usernote")] string type = "", [Summary("user")] string badUser = "")
        {
            IQueryable warnings = null;
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(badUser) || (!type.ToLower().Equals("warning") && !type.ToLower().Equals("usernote"))) // invalid parameters
            {
                Embed embed = CreateDescriptionEmbed($"💾 Usage: `unforgive [warning/usernote] [user]`");
                await SendEmbed(embed);
                return;
            }
            IUser user = resolveUser(badUser);
            ulong uID;
            if (user != null)
            {
                uID = user.Id;
            }
            else if (Regex.IsMatch(badUser, @"\d{17,18}"))
            {
                uID = Convert.ToUInt64(badUser);
            }
            else // user not in server AND not in database
            {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{badUser}\"");
                return;
            }

            if (type == "warning") // forgive warning
            {
                if (!_floofDB.Warnings.AsQueryable().Where(w => w.UserId == uID && w.GuildId == Context.Guild.Id && w.Forgiven == true).Any()) // there are no warnings for this user in this guild
                {
                    await Context.Channel.SendMessageAsync("User has no warnings to unforgive!");
                    return;
                }
                warnings = _floofDB.Warnings.AsQueryable()
                    .Where(u => u.UserId == uID && u.GuildId == Context.Guild.Id && u.Forgiven == true)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }
            else if (type == "usernote") // forgive usernote
            {
                if (!_floofDB.UserNotes.AsQueryable().Where(w => w.UserId == uID && w.GuildId == Context.Guild.Id && w.Forgiven == true).Any()) // there are no user notes for this user in this guild
                {
                    await Context.Channel.SendMessageAsync("User has no warnings to unforgive!");
                    return;
                }
                warnings = _floofDB.UserNotes.AsQueryable()
                    .Where(u => u.UserId == uID && u.GuildId == Context.Guild.Id && u.Forgiven == true)
                    .OrderByDescending(x => x.DateAdded).Take(10);
            }

            // handle user input
            EmbedBuilder builder = new EmbedBuilder();
            builder.Color = ADMIN_COLOR;
            if (user == null) // no user, just id in database
                builder.WithTitle($"Forgiven warnings for {badUser}");
            else
                builder.WithTitle($"Forgiven warnings for {user.Username}#{user.Discriminator}");
            if (warnings == null) // for some reason didnt recieve data from database
            {
                return;
            }
            if (type == "warning")
            {
                foreach (Warning w in warnings)
                {
                    builder.AddField($"**ID: {w.Id}** - {w.DateAdded.ToString("yyyy MMMM dd")} - {w.Moderator}", $"```{w.Reason}```");
                }
            }
            else if (type == "usernote")
            {
                foreach (UserNote w in warnings)
                {
                    builder.AddField($"**ID: {w.Id}** - {w.DateAdded.ToString("yyyy MMMM dd")} - {w.Moderator}", $"```{w.Reason}```");
                }
            }
            await SendEmbed(builder.Build());
            try
            {
                await ReplyAsync("Which would you like to unforgive? Please specify the ID.");

                SocketMessage response = await NextMessageAsync(true, true, TimeSpan.FromSeconds(10)); // wait for reply from source user in source channel for 2 seconds
                if (response == null)
                {
                    await Context.Channel.SendMessageAsync("You did not respond in time. Aborting...");
                    return;
                }

                ulong warningId;
                if (ulong.TryParse(response.Content, out warningId)) // response is of type integer
                {
                    if (type == "warning")
                    {
                        foreach (Warning w in warnings)
                        {
                            if (w.Id == warningId)
                            {
                                var modId = Context.Message.Author.Id;
                                var modUsername = $"{Context.Message.Author.Username}#{Context.Message.Author.Discriminator}";

                                await SetWarningForgivenStatus(w, false, 0);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has unforgiven the warning with the ID {w.Id} and the reason: {w.Reason}.");
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
                                await SetUserNoteForgivenStatus(un, false, 0);
                                await Context.Channel.SendMessageAsync($"Got it! {modUsername} has unforgiven the user note with the ID {un.Id} and the reason: {un.Reason}.");
                                return;
                            }
                        }
                    }
                    await Context.Channel.SendMessageAsync("You have provided either an incorrect response, or that warning ID is not in the list of warnings. Aborting...");
                    return;
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Invalid input, please provide a valid number. Aborting..");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }

        [Command("mute")]
        [Summary("Applies a mute role to a user")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task MuteUser([Summary("user")]string user, [Summary("Time")]string time = null)
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            IRole muteRole;

            //check to see if the server exists within the "AdminConfig" Table
            if (!_floofDB.AdminConfig.AsQueryable().Any(x => x.ServerId == Context.Guild.Id)) {

                //create new mute role
                muteRole = await CreateMuteRole();

                //save the newly created role
                _floofDB.Add(new AdminConfig {
                    ServerId = Context.Guild.Id,
                    MuteRoleId = muteRole.Id
                });
                _floofDB.SaveChanges();
            }
            else {
                //grabs the mute role from the database
                muteRole = Context.Guild.GetRole(
                   _floofDB.AdminConfig.AsQueryable()
                   .Where(x => x.ServerId == Context.Guild.Id)
                   .Select(x => x.MuteRoleId).ToList()[0]);

                //mute role was deleted create a new one
                if (muteRole == null) {
                    muteRole = await CreateMuteRole();
                    var result = _floofDB.AdminConfig.AsQueryable()
                         .SingleOrDefault(x => x.ServerId == Context.Guild.Id);
                    result.MuteRoleId = muteRole.Id;
                    _floofDB.SaveChanges();
                }
            }

            if (Context.Guild.GetUser(badUser.Id).Roles.Contains(muteRole)) {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is already muted!");
                return;
            }

            await Context.Guild.GetUser(badUser.Id).AddRoleAsync(muteRole);

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔇 User Muted",
                Description = $"{badUser.Username}#{badUser.Discriminator} Muted!",
                Color = ADMIN_COLOR
            };

            string durationNotifyString = null;
            if (time != null) {
                var m = Regex.Match(time, @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

                int dd = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
                int hs = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
                int ms = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
                int ss = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;

                int seconds = dd * 86400 + hs * 60 * 60 + ms * 60 + ss;

                if (seconds > 0) {
                    TimeSpan duration = TimeSpan.FromSeconds(seconds);

                    string delyString = "";

                    if (duration.Days > 0)
                        delyString += $"Days: {duration.Days} ";
                    if (duration.Hours > 0)
                        delyString += $"Hours: {duration.Hours} ";
                    if (duration.Minutes > 0)
                        delyString += $"Minutes: {duration.Minutes} ";
                    if (duration.Seconds > 0)
                        delyString += $"Seconds: {duration.Seconds} ";

                    durationNotifyString = delyString;
                    builder.AddField("Duration", delyString);
                    //unmute user after duration has expired
                    await Task.Run(async () =>
                    {
                        await Task.Delay(duration);

                        if (Context.Guild.GetUser(badUser.Id).Roles.Contains(muteRole)) {
                            await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(muteRole);

                            //notify user that they were unmuted
                            builder = new EmbedBuilder();
                            builder.Title = "🔊  Unmute Notification";
                            builder.Description = $"Your Mute on {Context.Guild.Name} has expired";
                            builder.Color = ADMIN_COLOR;
                            await badUser.SendMessageAsync("", false, builder.Build());
                        }
                    });

                }
                else {
                    await Context.Channel.SendMessageAsync("Invalid Time format... \nExamples: `.mute Talon#6237 1d` `.mute Talon#6237 6h30m`");
                    return;
                }

            }
            await Context.Channel.SendMessageAsync("", false, builder.Build());

            //notify user that they were muted
            builder = new EmbedBuilder();
            builder.Title = "🔇  Mute Notification";
            builder.Description = $"You have been muted on {Context.Guild.Name}";

            if (durationNotifyString != null)
                builder.AddField("Duration", durationNotifyString);

            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());
        }

        public async Task<IRole> CreateMuteRole()
        {
            var muteRole = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(), Color.DarkerGrey, false, false);

            //add channel overrides for the new mute role
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
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            var muteRole = Context.Guild.GetRole(
                   _floofDB.AdminConfig.AsQueryable()
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

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔊 User Unmuted",
                Description = $"{badUser.Username}#{badUser.Discriminator} was unmuted!",
                Color = ADMIN_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());

            //notify user that they were unmuted
            builder = new EmbedBuilder();
            builder.Title = "🔊  Unmute Notification";
            builder.Description = $"Your Mute on {Context.Guild.Name} has expired";
            builder.Color = ADMIN_COLOR;
            await badUser.SendMessageAsync("", false, builder.Build());
        }

        [Command("lock")]
        [Summary("Locks a channel")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task ChannelLock()
        {
            try {
                IGuildChannel textChannel = (IGuildChannel)Context.Channel;
                EmbedBuilder builder = new EmbedBuilder {
                    Description = $"🔒  <#{textChannel.Id}> Locked",
                    Color = Color.Orange,

                };
                foreach (IRole role in Context.Guild.Roles.Where(r => !r.Permissions.ManageMessages)) {
                    var perms = textChannel.GetPermissionOverwrite(role).GetValueOrDefault();

                    await textChannel.AddPermissionOverwriteAsync(role, perms.Modify(sendMessages: PermValue.Deny));
                }
                await Context.Channel.SendMessageAsync("", false, builder.Build());
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
                IGuildChannel textChannel = (IGuildChannel)Context.Channel;
                EmbedBuilder builder = new EmbedBuilder {
                    Description = $"🔓  <#{textChannel.Id}> Unlocked",
                    Color = Color.DarkGreen,

                };
                foreach (IRole role in Context.Guild.Roles.Where(r => !r.Permissions.ManageMessages)) {
                    var perms = textChannel.GetPermissionOverwrite(role).GetValueOrDefault();
                    if (role.Name != "nadeko-mute" && role.Name != "Muted")
                        await textChannel.AddPermissionOverwriteAsync(role, perms.Modify(sendMessages: PermValue.Allow));
                }
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            catch {
                await Context.Channel.SendMessageAsync("Something went wrong!");
            }
        }

        // r/furry Discord Rules Gate
        [Command("ireadtherules")]
        [Summary("Confirms a user has read the server rules by giving them a new role")]
        public async Task getaccess()
        {
            ulong serverId = 225980129799700481;
            ulong readRulesRoleId = 494149550622375936;
            if (Context.Guild.Id == serverId) {
                var user = (IGuildUser)Context.User;
                await user.AddRoleAsync(Context.Guild.GetRole(readRulesRoleId));
            }
            await Context.Message.DeleteAsync();
        }

        private IUser resolveUser(string input)
        {
            IUser user = null;
            //resolve userID or @mention
            if (Regex.IsMatch(input, @"\d{17,18}")) {
                string userID = Regex.Match(input, @"\d{17,18}").Value;
                user = Context.Client.GetUser(Convert.ToUInt64(userID));
            }
            //resolve username#0000
            else if (Regex.IsMatch(input, ".*#[0-9]{4}")) {
                string[] splilt = input.Split("#");
                user = Context.Client.GetUser(splilt[0], splilt[1]);
            }
            return user;
        }
        private Embed CreateDescriptionEmbed(string description)
        {
            EmbedBuilder builder = new EmbedBuilder
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
        private async Task SetWarningForgivenStatus(Warning w, bool status, ulong forgivenBy)
        {
            w.Forgiven = status;
            w.ForgivenBy = forgivenBy;
            await _floofDB.SaveChangesAsync();
        }
        private async Task SetUserNoteForgivenStatus(UserNote un, bool status, ulong forgivenBy)
        {
            un.Forgiven = status;
            un.ForgivenBy = forgivenBy;
            await _floofDB.SaveChangesAsync();
        }
    }
}
