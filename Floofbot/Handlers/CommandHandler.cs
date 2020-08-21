using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Discord.Commands;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Floofbot.Handlers
{
    public class CommandHandler
    {
        private DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _services = BuildServiceProvider(client);
            var context = _services.GetRequiredService<FloofDataContext>();
            context.Database.Migrate(); // apply all migrations
            _commands = new CommandService(new CommandServiceConfig { CaseSensitiveCommands = false });
            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private IServiceProvider BuildServiceProvider(DiscordSocketClient client)
        {
            InteractiveService interactiveService = new InteractiveService(client);
            return new ServiceCollection()
                .AddSingleton<InteractiveService>(interactiveService)
                .AddDbContext<FloofDataContext>()
                .BuildServiceProvider();
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            var context = new SocketCommandContext(_client, msg);
            int argPos = 0;
            string prefix;

            if (string.IsNullOrEmpty(BotConfigFactory.Config.Prefix))
            {
                prefix = ".";
                Log.Warning($"Defaulting to prefix '{prefix}' since no prefix specified!");
            }
            else
            {
                prefix = BotConfigFactory.Config.Prefix;
            }

            if (msg.HasStringPrefix(prefix, ref argPos) || msg.HasMentionPrefix(_client.CurrentUser, ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    switch (result.Error)
                    {
                        case CommandError.BadArgCount:
                            await msg.Channel.SendMessageAsync("ERROR: ``Too many or too few arguments. Please use the help command to view the required arguments.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.MultipleMatches:
                            await msg.Channel.SendMessageAsync("ERROR: ``Multiple commands with the same name. I don't know what command you want me to do!``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.ObjectNotFound:
                            await msg.Channel.SendMessageAsync("ERROR: ``The specified argument does not match the expected object. Examples include supplying a user tag when a discord channel is expected as an argument.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.ParseFailed:
                            await msg.Channel.SendMessageAsync("ERROR: ``For some reason, I am unable to parse your command.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.UnknownCommand:
                            await msg.Channel.SendMessageAsync("ERROR: ``Unknown command. Please check your spelling and try again.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.UnmetPrecondition:
                            await msg.Channel.SendMessageAsync("ERROR: ``The command may not have completed successfully as some preconditions were not met.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.Unsuccessful:
                            await msg.Channel.SendMessageAsync("ERROR: ``For some reason, I am unable to execute that command at the moment. Try again.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        case CommandError.Exception:
                            await msg.Channel.SendMessageAsync("Error: ``An exception occured when running that command.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                        default:
                            await msg.Channel.SendMessageAsync("Error: ``An unknown exception occured.``");
                            Log.Error(result.Error + ": " + result.ErrorReason);
                            break;
                    }
                    Log.Error(result.ErrorReason);
                }
            }
        }
    }
}
