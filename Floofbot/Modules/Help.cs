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
    [Summary("Commands for Listing Available Commands")]
    public class Help : InteractiveBase
    {
        private readonly IServiceProvider _services;
        private readonly CommandService _commands;
        public Help(CommandService commands, IServiceProvider services)
        {
            _services = services;
            _commands = commands;
        }

        [Summary("Show all available commands")]
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

        [Summary("Show help for a specific module")]
        [Command("help")]
        public async Task HelpCommand([Summary("module")]string requestedModule)
        {
            List<string> moduleNames = new List<string>();
            List<CommandInfo> commands = _commands.Commands.ToList();
            List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();
            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();

            // put available module names into a list instead of their object
            foreach (ModuleInfo module in _commands.Modules.ToList())
                moduleNames.Add(module.Name.ToLower());

            if (!moduleNames.Contains(requestedModule.ToLower())) // no such cmd
            {
                await Context.Channel.SendMessageAsync("Unable to find that command");
                return;
            }

            foreach (CommandInfo cmd in commands)
            {
                if (cmd.Module.Name.ToLower() == requestedModule.ToLower())
                {
                    List<ParameterInfo> parameters = cmd.Parameters.ToList(); // get all params

                    foreach (ParameterInfo param in parameters)
                    {
                        fields.Add(new EmbedFieldBuilder()
                        {
                            Name = $"{param.Name}",
                            Value = param.Summary ?? "No parameter description available",
                            IsInline = false
                        });
                    }
                    pages.Add(new PaginatedMessage.Page
                    {
                        Author = new EmbedAuthorBuilder { Name = cmd.Name },
                        Fields = new List<EmbedFieldBuilder>(fields),
                        Description = cmd.Summary ?? "No command description available"
                    });
                    fields.Clear();
                }
            }
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = Color.DarkGreen,
                Content = $"{Context.User.Mention} here are the commands available for {requestedModule}!",
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
