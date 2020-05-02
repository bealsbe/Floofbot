using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Floofbot.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace Floofbot
{
    public class Utilities : ModuleBase<SocketCommandContext>
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        public Utilities(CommandService commands, IServiceProvider services)
        {
            _services = services;
            _commands = commands;
        }

        [Command("help")]
        public async Task Help()
        {
            try
            {
                List<CommandInfo> commands = _commands.Commands.ToList();
                List<ModuleInfo> modules = _commands.Modules.ToList();
                EmbedBuilder embedBuilder = new EmbedBuilder();
                bool stopped = false;
                while (!stopped)
                foreach (ModuleInfo module in modules)
                {
                    EmbedBuilder embed = new EmbedBuilder();

                    embed.WithTitle(module.Name);
                    embed.WithDescription("``" + module.Summary ?? "No module description available" + "``");
                    embed.WithColor(Color.Green);
                    foreach (CommandInfo command in commands)
                    {
                        if (command.Module == module)
                        {
                            string aliases;
                            if (command.Aliases != null)
                                aliases = string.Join(", ", command.Aliases);
                            else
                                aliases = "None";

                            embed.AddField($"{command.Name} (aliases: {aliases})", module.Summary ?? "No command description available");
                        }
                    }
                    await Context.Channel.SendMessageAsync("", false, embed.Build());
                }
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        [Command("ping")]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync(":owl:").ConfigureAwait(false);
            sw.Stop();
            await msg.DeleteAsync();

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "Butts!",
                Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = Color.Magenta
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("userinfo")]
        [RequireContext(ContextType.Guild)]
        public async Task UserInfo(IGuildUser usr = null)
        {
            var user = usr ?? Context.User as IGuildUser;

            if (user == null)
                return;

            string avatar = "https://cdn.discordapp.com/attachments/440635657925165060/442039889475665930/Turqouise.jpg";

            if (user.AvatarId != null)
                avatar = user.GetAvatarUrl(ImageFormat.Auto, 512);

            string infostring = $"👥 **User info for:** {user.Mention}\n";
            infostring += 
                 $"**Username** : {user.Username}#{user.Discriminator}\n" +
                 $"**Nickname** : {user.Nickname ?? user.Username}\n" +
                 $"**ID** : {user.Id}\n" +
                 $"**Discord Join Date** : {user.CreatedAt:MM/dd/yyyy} \n" +
                 $"**Guild Join Date** : {user.JoinedAt?.ToString("MM/dd/yyyy")}\n" +
                 $"**Status** : {user.Status}\n";

            EmbedBuilder builder = new EmbedBuilder {
                ThumbnailUrl = avatar,
                Description = infostring,
                Color = Color.Magenta
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("avatar")]
        [RequireContext(ContextType.Guild)]
        public async Task Avatar([Remainder] IGuildUser user = null)
        {
            if (user == null)
                user = (IGuildUser)Context.User;

            var avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 512);
            EmbedBuilder builder = new EmbedBuilder() {
                Description = $"🖼️ **Avatar for:** {user.Mention}\n",
                ImageUrl = avatarUrl,
                Color = Color.Magenta

            };
            await Context.Channel.SendMessageAsync("", false, builder.Build());

        }
    }
}
