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
            try
            {
                List<CommandInfo> commands = _commands.Commands.ToList();
                List<ModuleInfo> modules = _commands.Modules.ToList();
                List<EmbedFieldBuilder> listOfFields = new List<EmbedFieldBuilder>();

                foreach (ModuleInfo module in modules)
                {
                    foreach (CommandInfo command in commands)
                    {
                        string aliases;
                        if (command.Aliases != null)
                            aliases = string.Join(", ", command.Aliases);
                        else
                            aliases = "None";

                        listOfFields.Add(new EmbedFieldBuilder()
                        {
                            Name = module.Name,
                            Value = $"{command.Name} (aliases: {aliases})\n -> " + command.Summary ?? "No command description available",
                            IsInline = false
                        });
                    }
                }
                PaginatedMessage message = new PaginatedMessage
                {
                    Color = Discord.Color.Green,
                    Pages = listOfFields
                };
                await PagedReplyAsync(message, true);

            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }
    }
}
