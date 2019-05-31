using System;
using Floofbot.Services;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Floofbot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;
        private BotDatabase _botDatabase;

        static void Main(string[] args)
        {
            string token = "";
            if (!(args.Length == 1)) {
                Console.WriteLine("Enter Bot Token");
                token = Console.ReadLine();
            }
            else {
                token = args[0];
            }
            new Program().MainAsync(token).GetAwaiter().GetResult();
        }

        public async Task MainAsync(string token)
        {
            _client = new DiscordSocketClient(
                  new DiscordSocketConfig() {
                      LogLevel = LogSeverity.Info
                  });
            try {
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message + "\nPress any key to exit");
                Console.ReadKey();
                Environment.Exit(1);
            };
            _client.Log += (LogMessage message) =>
            {
                Console.WriteLine($"{DateTime.Now.ToString("[MM/dd/yyyy HH:mm]")} {message.Source}: {message.Message}");
                return Task.CompletedTask;
            };
            _botDatabase = new BotDatabase();
            _handler = new CommandHandler(_client);

            await Task.Delay(-1);
        }
    }
}
