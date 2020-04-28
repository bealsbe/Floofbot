using System;
using Floofbot.Services;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Sinks.File;

namespace Floofbot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;
        private BotDatabase _botDatabase;

        static async Task Main(string[] args)
        {
            InitialiseLogger();

            // TODO: Replace console input with env variable import
            string token;
            if (!(args.Length == 1)) {
                Console.WriteLine("Enter Bot Token");
                token = Console.ReadLine();
            }
            else {
                token = args[0];
            }
            await new Program().MainAsync(token);
        }

        private static void InitialiseLogger()
        {
            // Initialise the logger. Outputs Debug+ to console and Info+ to file
            string logTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fffzzz}] - {Level:u3}: {Message:lj}{NewLine}";
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File("FloofLog.log",
                    restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information,
                    rollOnFileSizeLimit: true,
                    outputTemplate: logTemplate)
                .WriteTo.Console(outputTemplate: logTemplate)
                .CreateLogger();
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
                Log.Error(ex.Message);
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
                Environment.Exit(1);
            };
            _client.Log += (LogMessage msg) =>
            {
                Log.Information("{Source}: {Message}", msg.Source, msg.Message);
                return Task.CompletedTask;
            };
            _botDatabase = new BotDatabase();
            _handler = new CommandHandler(_client);

            await Task.Delay(-1);
        }
    }
}
