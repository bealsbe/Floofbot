using Discord;

class BotConfig {
    public static readonly string BotPrefix = ".";

    public class BotActivity : IActivity {
        public string Name {
            get {
                return "🦉 Simulator";
            }
        }
        public ActivityType Type {
            get {
                return ActivityType.Playing;
            }
        }
        public ActivityProperties Flags {
            get {
                return ActivityProperties.None;
            }
        }
        public string Details {
            get {
                return string.Empty;
            }
        }
    }
}
