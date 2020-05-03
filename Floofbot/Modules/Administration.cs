using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Floofbot.Modules
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        private FloofDataContext _floofDB;
        public Administration(FloofDataContext floofDB) => _floofDB = floofDB;

        [Command("ban")]
        [Alias("b")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task YeetUser(string input, [Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(input);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{input}\"");
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
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task kickUser(string user, [Remainder]string reason = "No Reason Provided")
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
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnUser(string user, string reason)
        {
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
            EmbedBuilder builder = new EmbedBuilder();
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
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnlog(string user)
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
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task MuteUser(string user, string time = null)
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
                mute_role = await CreateMuteRole();

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
                    mute_role = await CreateMuteRole();
                    var result = _floofDB.AdminConfig.AsQueryable()
                         .SingleOrDefault(x => x.ServerId == Context.Guild.Id);
                    result.MuteRoleId = mute_role.Id;
                    _floofDB.SaveChanges();
                }


            }

            await Context.Guild.GetUser(badUser.Id).AddRoleAsync(mute_role);

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔇 User Muted",
                Description = $"{badUser.Username}#{badUser.Discriminator} Muted!",
                Color = Color.DarkBlue
            };


            if (time != null) {
                var m = Regex.Match(time, @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

                int dd = m.Groups["days"].Success ? int.Parse(m.Groups["days"].Value) : 0;
                int hs = m.Groups["hours"].Success ? int.Parse(m.Groups["hours"].Value) : 0;
                int ms = m.Groups["minutes"].Success ? int.Parse(m.Groups["minutes"].Value) : 0;
                int ss = m.Groups["seconds"].Success ? int.Parse(m.Groups["seconds"].Value) : 0;

                int seconds = dd * 86400 + hs * 60 * 60 + ms * 60 + ss;

                if (seconds > 0) {
                    TimeSpan duration = TimeSpan.FromSeconds(seconds);
                    builder.AddField("Duration", $"Days: {duration.Days} Hours: {duration.Hours} Minutes: {duration.Minutes} Seconds: {duration.Seconds}");

                 DateTime expires  =  DateTime.Now.AddSeconds(seconds);
                    builder.AddField("Expires", $"{expires.ToString("dddd, dd MMMM yyyy hh:mm tt")} PDT \n {expires.ToUniversalTime().ToString("dddd, dd MMMM yyyy hh:mm tt")} UTC");
                }
                else {
                    await Context.Channel.SendMessageAsync("Invalid Time format... \nExamples: `.mute Talon#6237 1d` `.mute Talon#6237 6h30m`");
                    return;
                }

            }

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        public async Task<IRole> CreateMuteRole()
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
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task UnMuteUser(string user)
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

            await Context.Guild.GetUser(badUser.Id).RemoveRoleAsync(mute_role);

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "🔊 User Unmuted",
                Description = $"{badUser.Username}#{badUser.Discriminator} was unmuted!",
                Color = Color.DarkBlue
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }


        public async Task UnmuteUser()
        {

        }

        [Command("lock")]
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

        // rfurry Discord Rules Gate
        [Command("ireadtherules")]
        public async Task getaccess()
        {
            if (Context.Guild.Id == 225980129799700481) {
                ulong roleID = 494149550622375936;
                var user = (IGuildUser)Context.User;
                await user.AddRoleAsync(Context.Guild.GetRole(roleID));
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
