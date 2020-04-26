using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;

namespace Floofbot.Modules
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task YeetUser(string input, [Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(input);
            EmbedBuilder builder = new EmbedBuilder();

            try {
                //sends message to user
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
                builder.AddField("Moderator",$"{Context.User.Username}#{Context.User.Discriminator}");

            }
            catch {
                builder = new EmbedBuilder();
                builder.Description = $"could not find user \"{input}\"";
                builder.Color = Color.Red;
            }

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("warn")]
        public async Task warnUser(string input)
        { 


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
