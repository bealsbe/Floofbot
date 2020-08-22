using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Floofbot.Configs;
using Floofbot.Handlers;
using Floofbot.Services;
using Serilog;

namespace Floofbot
{
    class Program
    {
        private DiscordSocketClient _client;
        private CommandHandler _handler;

        static async Task Main(string[] args)
        {
            string botDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            string _configPath;
            if (args.Length == 1)
                _configPath = args[0];
            else
                _configPath = botDirectory + "/app.config";


            InitialiseLogger();
            InitialiseConfig(_configPath);

            string token = BotConfigFactory.Config.Token;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Error: the Token field in app.config must contain a valid Discord bot token.");
                Environment.Exit(1);
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

        private static void InitialiseConfig(string _configPath)
        {
            BotConfigFactory.Initialize(_configPath);
        }

        public async Task MainAsync(string token)
        {
            _client = new DiscordSocketClient(
                  new DiscordSocketConfig()
                  {
                      LogLevel = LogSeverity.Info,
                      MessageCacheSize = 100
                  });

            try
            {
                var _EventLoggerService = new EventLoggerService(_client);
                await _client.LoginAsync(TokenType.Bot, token);
                await _client.StartAsync();
            }
            catch (Exception ex)
            {
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
            _handler = new CommandHandler(_client);

            if (BotConfigFactory.Config.Activity != null)
            {
                await _client.SetActivityAsync(BotConfigFactory.Config.Activity);
            }
            await Task.Delay(-1);
        }
    }
}
