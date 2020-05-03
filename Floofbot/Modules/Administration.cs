using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Linq;

namespace Floofbot.Modules
{
    [Summary("Administration commands")]
    public class Administration : ModuleBase<SocketCommandContext>
    {
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
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "⚖️ Ban Notification";
            builder.Description = $"You have been banned from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            //bans the user
            await Context.Guild.AddBanAsync(badUser.Id, 0, $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Banned");
            builder.Color = Color.DarkOrange;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
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
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            //kicks users
            await Context.Guild.GetUser(badUser.Id).KickAsync(reason);
            builder = new EmbedBuilder();
            builder.Title = ("🥾 User Kicked");
            builder.Color = Color.DarkOrange;
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

            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            _floofDB.Add(new Warning {
                DateAdded = DateTime.Now,
                Forgiven = false,
                GuildId = Context.Guild.Id,
                Moderator = Context.User.Id,
                Reason = reason,
                UserId = badUser.Id
            });
            _floofDB.SaveChanges();

            //sends message to user
            builder = new EmbedBuilder();
            builder.Title = "⚖️ Warn Notification";
            builder.Description = $"You have recieved a warning in {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Warned");
            builder.Color = Color.DarkOrange;
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
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            var warnings = _floofDB.Warnings.AsQueryable()
                .Where(u => u.UserId == badUser.Id && u.GuildId == Context.Guild.Id)
                .OrderByDescending(x => x.DateAdded).Take(24);

            EmbedBuilder builder = new EmbedBuilder();
            int warningCount = 0;
            builder.WithTitle($"Warnings for {badUser.Username}#{badUser.Discriminator}");
            foreach (Warning warning in warnings) {
                builder.AddField($"**{warningCount + 1}**. {warning.DateAdded.ToString("yyyy-MM-dd")}", $"```{warning.Reason}```");
                warningCount++;
            }
            await Context.Channel.SendMessageAsync("", false, builder.Build());
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

            IRole mute_role;

            //check to see if the server exists within the "AdminConfig" Table
            if (!_floofDB.AdminConfig.AsQueryable().Any(x => x.ServerId == Context.Guild.Id)) {

                //create new mute role
                mute_role = await createMuteRole();

                //save the newly created role
                _floofDB.Add(new AdminConfig {
                    ServerId = Context.Guild.Id,
                    MuteRoleId = mute_role.Id
                });
                _floofDB.SaveChanges();
            }
            else {
                //grabs the mute role from the database
                mute_role = Context.Guild.GetRole(
                   _floofDB.AdminConfig.AsQueryable()
                   .Where(x => x.ServerId == Context.Guild.Id)
                   .Select(x => x.MuteRoleId).ToList()[0]);

                //mute role was deleted create a new one
                if (mute_role == null) {
                    mute_role = await createMuteRole();
                    var result = _floofDB.AdminConfig.AsQueryable()
                         .SingleOrDefault(x => x.ServerId == Context.Guild.Id);
                    result.MuteRoleId = mute_role.Id;
                    _floofDB.SaveChanges();
                }
            }

            if (Context.Guild.GetUser(badUser.Id).Roles.Contains(mute_role)) {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is already muted!");
                return;
            }

                await Context.Guild.GetUser(badUser.Id).AddRoleAsync(mute_role);

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔇 User Muted",
                Description = $"{badUser.Username}#{badUser.Discriminator} Muted!",
                Color = Color.DarkBlue
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
                    Task.Run(async () => {
                        await Task.Delay(duration);

                        if (Context.Guild.GetUser(badUser.Id).Roles.Contains(mute_role)) {
                            await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(mute_role);

                            //notify user that they were unmuted
                            builder = new EmbedBuilder();
                            builder.Title = "🔊  Unmute Notification";
                            builder.Description = $"Your Mute on {Context.Guild.Name} has expired";
                            builder.Color = Color.DarkBlue;
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

            builder.Color = Color.DarkBlue;
            await badUser.SendMessageAsync("", false, builder.Build());
        }

        public async Task<IRole> createMuteRole()
        {
            var mute_role = await Context.Guild.CreateRoleAsync("Muted", new GuildPermissions(), Color.DarkerGrey, false, false);

            //add channel overrides for the new mute role
            foreach (IGuildChannel channel in Context.Guild.Channels) {
                OverwritePermissions permissions = new OverwritePermissions(
                    sendMessages: PermValue.Deny,
                    addReactions: PermValue.Deny,
                    speak: PermValue.Deny
                    );

                await channel.AddPermissionOverwriteAsync(mute_role, permissions);
            }

            return mute_role;
        }

        [Command("unmute")]
        [Summary("Removes a mute role from a user")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task UnMuteUser([Summary("user")]string user)
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            var mute_role = Context.Guild.GetRole(
                   _floofDB.AdminConfig.AsQueryable()
                   .Where(x => x.ServerId == Context.Guild.Id)
                   .Select(x => x.MuteRoleId).ToList()[0]);

            if (mute_role == null) {
                await Context.Channel.SendMessageAsync("The Mute Role for this Server Doesn't Exist!\n" +
                    "A new one will be created next time you run the `mute` command");
                return;
            }

            if (Context.Guild.GetUser(badUser.Id).Roles.Contains(mute_role)) {
                await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(mute_role);
            }
            else {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is not muted");
                return;
            }

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔊 User Unmuted",
                Description = $"{badUser.Username}#{badUser.Discriminator} was unmuted!",
                Color = Color.DarkBlue
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());

            //notify user that they were unmuted
            builder = new EmbedBuilder();
            builder.Title = "🔊  Unmute Notification";
            builder.Description = $"Your Mute on {Context.Guild.Name} has expired";
            builder.Color = Color.DarkBlue;
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
    }
}
