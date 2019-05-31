using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

namespace Floofbot.Services
{
    public class CommandHandler
    {

        private DiscordSocketClient _client;
        private CommandService _service;
        private IServiceProvider _services;

        public CommandHandler(DiscordSocketClient client)
        {
            _client = client;
            _service = new CommandService();
            _service.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            _client.MessageReceived += HandleCommandAsync;
        }

        private async Task HandleCommandAsync(SocketMessage s)
        {
            var msg = s as SocketUserMessage;
            if (msg == null)
                return;

            var context = new SocketCommandContext(_client, msg);
            int argPos = 0;

            if (msg.HasCharPrefix('.', ref argPos)) {
                var result = await _service.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand) {
                    Console.WriteLine(result.ErrorReason);
                }
            }
        }
    }
}
