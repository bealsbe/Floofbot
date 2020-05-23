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

        [YamlMember(Alias = "Database", ApplyNamingConventions = false)]
        public BotDbConnection DbConnection { get; set; }

        public List<BotRandomResponse> RandomResponses { get; set; }
    }
}
