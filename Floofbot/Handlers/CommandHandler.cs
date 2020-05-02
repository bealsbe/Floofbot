using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Floofbot.Services.Repository;
using Microsoft.EntityFrameworkCore;

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
            _services = BuildServiceProvider();
            var context = _services.GetRequiredService<FloofDataContext>();
            context.Database.Migrate(); // apply all migrations
            _commands = new CommandService();
            _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private IServiceProvider BuildServiceProvider()
        {
            return new ServiceCollection()
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

            if (msg.HasCharPrefix('.', ref argPos))
            {
                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    Log.Error(result.ErrorReason);
                }
            }
            else if (msg.Source == MessageSource.User)
            {
                var generator = new RandomResponseGenerator();
                string randomResponse = generator.generateResponse(msg);

                if (!string.IsNullOrEmpty(randomResponse))
                {
                    await context.Channel.SendMessageAsync(randomResponse);
                }
            }
        }
    }
}
