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
using System.Linq;
using Discord.Rest;
using System.Text.RegularExpressions;

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
            _client.MessageUpdated += OnMessageUpdatedHandler;
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
            if (msg.Channel.GetType() != typeof(SocketDMChannel))
                author.Url = msg.GetJumpUrl();

            EmbedBuilder builder = new EmbedBuilder
            {
                Author = author,
                Title = "A fatal error has occured. User message content: " + msg.Content,
                Description = result.Error + "\n```" + result.ErrorReason + "```",
                Color = Color.Red
            };
            builder.AddField("Channel Type", (msg.Channel.GetType() == typeof(SocketDMChannel) ? "DM" : "Guild") + " Channel");

            builder.WithCurrentTimestamp();
            return builder.Build();
        }

        private async Task LogErrorInDiscordChannel(IResult result, SocketMessage originalMessage)
        {
            FloofDataContext _floofDb = new FloofDataContext();

            var userMsg = originalMessage as SocketUserMessage; // the original command
            var channel = userMsg.Channel as ITextChannel; // the channel of the original command
            if (channel == null)
                return;

            var serverConfig = _floofDb.ErrorLoggingConfigs.Find(channel.GuildId); // no db result
            if (serverConfig == null)
                return;

            if ((!serverConfig.IsOn) || (serverConfig.ChannelId == null)) // not configured or disabled
                return;

            Discord.ITextChannel errorLoggingChannel = await channel.Guild.GetTextChannelAsync((ulong)serverConfig.ChannelId); // can return null if channel invalid
            if (errorLoggingChannel == null)
                return;


            Embed embed = generateErrorEmbed(userMsg.Author, result, userMsg);
            await errorLoggingChannel.SendMessageAsync("", false, embed);
            return;
        }
        private async Task OnMessageUpdatedHandler(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel chan)
        {
            var messageBefore = before.Value as IUserMessage;
            if (messageBefore == null)
                return;

            if (messageBefore.EditedTimestamp == null) // user has never edited their message
            {
                var timeDifference = DateTimeOffset.Now - messageBefore.Timestamp;
                if (timeDifference.TotalSeconds < 30)
                    await HandleCommandAsync(after);

            }
        }
        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            if (msg.Author.IsBot)
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

            bool hasValidPrefix = msg.HasMentionPrefix(_client.CurrentUser, ref argPos) || msg.HasStringPrefix(prefix, ref argPos);
            string strippedCommandName = msg.Content.Substring(argPos).Split()[0];
            bool hasValidStart = !string.IsNullOrEmpty(strippedCommandName) && Regex.IsMatch(strippedCommandName, @"^[0-9]?[a-z]+\??$", RegexOptions.IgnoreCase);
            if (hasValidPrefix && hasValidStart)
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess)
                {
                    string errorMessage = "An unknown exception occured. I have notified the administrators.";
                    bool isCriticalFailure = false;
                    switch (result.Error)
                    {
                        case CommandError.BadArgCount:
                            errorMessage = result.ErrorReason;
                            break;
                        case CommandError.MultipleMatches:
                            errorMessage = "Multiple commands with the same name. I don't know what command you want me to do!";
                            break;
                        case CommandError.ObjectNotFound:
                            errorMessage = "The specified argument does not match the expected object - " + result.ErrorReason;
                            break;
                        case CommandError.ParseFailed:
                            errorMessage = "For some reason, I am unable to parse your command.";
                            break;
                        case CommandError.UnknownCommand:
                            // check 8ball response
                            if (msg.HasMentionPrefix(_client.CurrentUser, ref argPos) && msg.Content.EndsWith("?"))
                            {
                                string eightBallResponse = Floofbot.Modules.Helpers.EightBall.GetRandomResponse();

                                Embed embed = new EmbedBuilder()
                                {
                                    Description = msg.Content
                                }.Build();
                                await msg.Channel.SendMessageAsync($"{msg.Author.Mention} {eightBallResponse}", false, embed);
                                return;
                            }
                            else
                            {
                                string randomResponse = RandomResponseGenerator.GenerateResponse(msg);
                                if (!string.IsNullOrEmpty(randomResponse))
                                {
                                    await msg.Channel.SendMessageAsync(randomResponse);
                                    return;
                                }
                            }
                            errorMessage = "Unknown command '" + strippedCommandName + "'. Please check your spelling and try again.";
                            break;
                        case CommandError.UnmetPrecondition:
                            errorMessage = "You did not meet the required precondition - " + result.ErrorReason;
                            break;
                        default:
                            await LogErrorInDiscordChannel(result, msg);
                            isCriticalFailure = true;
                            break;
                    }
                    await msg.Channel.SendMessageAsync("ERROR: ``" + errorMessage + "``");
                    if (isCriticalFailure)
                        Log.Error(result.Error + "\nMessage Content: " + msg.Content + "\nError Reason: " + result.ErrorReason);
                    else
                        Log.Information(result.Error + "\nMessage Content: " + msg.Content + "\nError Reason: " + result.ErrorReason);
                }
            }
        }
    }
}
