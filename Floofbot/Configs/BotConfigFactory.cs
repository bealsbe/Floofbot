using System.IO;
using System.Runtime.CompilerServices;
using YamlDotNet.Serialization;

namespace Floofbot.Configs {
    class BotConfigFactory {
        private static BotConfig _config;
        private static string _filename;

        public static BotConfig Config {
            [MethodImpl(MethodImplOptions.Synchronized)]
            get {
                if (_config == null) {
                    _config = BotConfigParser.ParseFromFile(_filename);
                }
                return _config;
            }
        }

        public static void Initialize(string filename) {
            _filename = filename;
        }

        class BotConfigParser {
            public static BotConfig ParseFromFile(string filename) {
                var fileContents = new StringReader(File.ReadAllText(filename));
                var deserializer = new Deserializer();
                var config = deserializer.Deserialize<BotConfig>(fileContents);
                return config;
            }
        }
    }
}
