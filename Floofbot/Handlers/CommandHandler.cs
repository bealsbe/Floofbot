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

        private Embed generateErrorEmbed(SocketUser user, IResult result, SocketUserMessage msg)
        {
            EmbedAuthorBuilder author = new EmbedAuthorBuilder();
            author.Name = user.Username + "#" + user.Discriminator;
            if (Uri.IsWellFormedUriString(user.GetAvatarUrl(), UriKind.Absolute))
                author.IconUrl = user.GetAvatarUrl();
            author.Url = msg.GetJumpUrl();

            EmbedBuilder builder = new EmbedBuilder
            {
                Author = author,
                Title = "A fatal error has occured.",
                Description = result.Error + "\n```" + result.ErrorReason + "```",
                Color = Color.Red
            };
            builder.WithCurrentTimestamp();
            return builder.Build();
        }

        private async Task LogErrorInDiscordChannel(IResult result, SocketMessage originalMessage)
        {
            FloofDataContext _floofDb = new FloofDataContext();
            var userMsg = originalMessage as SocketUserMessage; // the original command
            if (userMsg == null)
                return;

            var channel = userMsg.Channel as ITextChannel; // the channel of the original command
            if (channel == null)
                return;

            var serverConfig = _floofDb.ErrorLoggingConfigs.Find(channel.GuildId); // no db result
            if (serverConfig == null)
                return;

            if ((!serverConfig.IsOn) || (serverConfig.ChannelId == 0)) // not configured or disabled
                return;

            Discord.ITextChannel errorLoggingChannel = await channel.Guild.GetTextChannelAsync((ulong)serverConfig.ChannelId); // can return null if channel invalid
            if (errorLoggingChannel == null)
                return;


            Embed embed = generateErrorEmbed(userMsg.Author, result, userMsg);
            await errorLoggingChannel.SendMessageAsync("", false, embed);
            return;
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
                    string errorMessage = "ERROR: ``An unknown exception occured. I have notified the administrators.``";
                    switch (result.Error)
                    {
                        case CommandError.BadArgCount:
                            errorMessage = "ERROR: ``" + result.ErrorReason + "``";
                            break;
                        case CommandError.MultipleMatches:
                            errorMessage = "ERROR: ``Multiple commands with the same name. I don't know what command you want me to do!``";
                            break;
                        case CommandError.ObjectNotFound:
                            errorMessage = "ERROR: ``The specified argument does not match the expected object - " + result.ErrorReason +"``";
                            break;
                        case CommandError.ParseFailed:
                            errorMessage = "ERROR: ``For some reason, I am unable to parse your command.``";
                            break;
                        case CommandError.UnknownCommand:
                            errorMessage = "ERROR: ``Unknown command. Please check your spelling and try again.``";
                            break;
                        case CommandError.UnmetPrecondition:
                            errorMessage = "ERROR: ``The command may not have completed successfully as some preconditions were not met.``";
                            break;
                        default:
                            await LogErrorInDiscordChannel(result, msg);
                            break;
                    }
                    await msg.Channel.SendMessageAsync(errorMessage);
                    Log.Error(result.Error + ": " + result.ErrorReason);
                }
            }
        }
    }
}
