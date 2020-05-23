using System;
using System.IO;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;
using Serilog;

namespace Floofbot.Configs
{
    class BotConfigFactory
    {
        private static BotConfig _config;
        private static string _filename;
        private static string _token;

        public static BotConfig Config
        {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get
            {
                if (_config == null)
                {
                    _config = BotConfigParser.ParseFromFile(_filename);
                    _token = _config.Token;
                }
                return _config;
            }
            [MethodImpl(MethodImplOptions.Synchronized)]
            private set
            {
                _config = value;
            }
        }

        public static void Initialize(string filename)
        {
            _filename = filename;
        }

        public static void Reinitialize()
        {
            BotConfig config = BotConfigParser.ParseFromFile(_filename);
            // sanity check to make sure the token was not changed upon reload
            if (config.Token == _token)
            {
                _config = config;
            }
            else
            {
                throw new InvalidDataException("Failed to reinitialize config file. Was the token field changed?");
            }
        }

        class BotConfigParser
        {
            public static BotConfig ParseFromFile(string filename)
            {
                try
                {
                    var fileContents = new StringReader(File.ReadAllText(filename));
                    var deserializer = new Deserializer();
                    var config = deserializer.Deserialize<BotConfig>(fileContents);
                    return config;
                }
                catch (Exception e)
                {
                    Log.Error("There was an error deserializing the file: " + e);
                    return new BotConfig();
                }
            }
        }
    }
}
