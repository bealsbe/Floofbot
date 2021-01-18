using Discord;
using System.Collections.Generic;

namespace Floofbot.Modules.Helpers
{
    class ChannelOverwriteCache
    {
        private Dictionary<ulong, IReadOnlyCollection<Overwrite>> channelPermissionOverwrites = new Dictionary<ulong, IReadOnlyCollection<Overwrite>>();

        public void AddItemToCache(ulong channelId, IReadOnlyCollection<Overwrite> overwrites)
        {
            if (!channelPermissionOverwrites.ContainsKey(channelId))
            {
                channelPermissionOverwrites.Add(channelId, overwrites);
            }
        }
        public void RemoveItemFromCache(ulong channelId)
        {
            if (channelPermissionOverwrites.ContainsKey(channelId))
            {
                channelPermissionOverwrites.Remove(channelId);
            }
        }
        public IReadOnlyCollection<Overwrite> GetEntry(ulong channelId)
        {
            if (channelPermissionOverwrites.ContainsKey(channelId))
                return channelPermissionOverwrites[channelId];
            else
                return null;
        }

    }
}
