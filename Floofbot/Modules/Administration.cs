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
        public async Task YeetUser(string input)
        {
            IUser badUser = null;
            bool goodInput = false;

            if (Regex.IsMatch(input, @"\d{17,18}")) {
                badUser = Context.Client.GetUser(Convert.ToUInt64(input));
                goodInput = true;
            }
            else if (Regex.IsMatch(input, ".*#[0-9]{4}")) {
                string[] splilt = input.Split("#");
                badUser = Context.Client.GetUser(splilt[0], splilt[1]);
                goodInput = true;
            }

            EmbedBuilder builder = new EmbedBuilder();
            if (goodInput) {
                //sends message to user
                builder.Title = "⚖️ Ban Notification";
                builder.Description = $"You have been banned from {Context.Guild.Name}";
                await badUser.SendMessageAsync("", false, builder.Build());

                //bans the user
                await Context.Guild.AddBanAsync(badUser.Id);
                await Context.Channel.SendMessageAsync($"Banned User: {badUser.Mention}");
            }
            else {
                //change this later
                await Context.Channel.SendMessageAsync("Bad Input");
            }
        }

        [Command("warn")]
        public async Task warnUser(string input)
        {



        }
    }
}
