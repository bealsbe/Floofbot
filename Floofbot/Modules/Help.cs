using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    public class Help : InteractiveBase
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        public Help(CommandService commands, IServiceProvider services)
        {
            _services = services;
            _commands = commands;
        }

        [Command("help")]
        public async Task HelpCommand()
        {
            List<CommandInfo> commands = _commands.Commands.ToList();
            List<ModuleInfo> modules = _commands.Modules.ToList();
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();

            foreach (ModuleInfo module in modules)
            {
                foreach (CommandInfo command in commands)
                {
                    if (command.Module == module)
                    {
                        string aliases;
                        if (command.Aliases != null)
                            aliases = string.Join(", ", command.Aliases);
                        else
                            aliases = "None";

                        fields.Add(new EmbedFieldBuilder()
                        {
                            Name = $"{command.Name} (aliases: {aliases})",
                            Value = command.Summary ?? "No command description available",
                            IsInline = false
                        });
                    }
                }
                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = module.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    Description = module.Summary ?? "No module description available"
                });
                fields.Clear();
            }
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = Color.DarkGreen,
                Content = Context.User.Mention,
                FooterOverride = null,
                ThumbnailUrl = Context.Client.CurrentUser.GetAvatarUrl(),
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
    }
}
