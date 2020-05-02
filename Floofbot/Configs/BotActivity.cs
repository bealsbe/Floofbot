using System;
using System.Configuration;
using Discord;

namespace Floofbot.Configs {
    public class BotActivity : IActivity {
        public string Name {
            get {
                return ConfigurationManager.AppSettings["ActivityName"];
            }
        }
        public ActivityType Type {
            get {
                string type = ConfigurationManager.AppSettings["ActivityType"];
                return Enum.Parse<ActivityType>(type);
            }
        }
        public ActivityProperties Flags {
            get {
                string flags = ConfigurationManager.AppSettings["ActivityFlags"];
                return Enum.Parse<ActivityProperties>(flags);
            }
        }
        public string Details {
            get {
                return ConfigurationManager.AppSettings["ActivityDetails"];
            }
        }
    }
}
