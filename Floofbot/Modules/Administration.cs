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
        [Summary("Bans a user from the server")]
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
        [Summary("Kicks a user from the server")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task kickUser(string user, [Remainder] string reason = "No Reason Provided")
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
        public async Task warnUser(string user, [Remainder] string reason = "")
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

        // rfurry Discord Rules Gate
        [Command("ireadtherules")]
        [Summary("Allows a user into the server")]
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
