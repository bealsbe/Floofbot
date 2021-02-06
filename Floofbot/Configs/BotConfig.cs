using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Floofbot.Configs
{
    class BotConfig
    {
        public string Token { get; set; }
        public string Prefix { get; set; }

        [YamlMember(Alias = "Activity", ApplyNamingConventions = false)]
        public BotActivity Activity { get; set; }

        public List<BotRandomResponse> RandomResponses { get; set; }

        public ulong ModMailServer { get; set; }
        public string DbPath { get; set; }
        public string BackupOutputPath { get; set; }
        public string BackupScript { get; set; }
        public int NumberOfBackups { get; set; }
        public List<ulong> AnnouncementChannels { get; set; }
        public Dictionary <string, int> RaidProtection { get; set; }
        public Dictionary <string, string> RulesGate { get; set; }

    }
}
