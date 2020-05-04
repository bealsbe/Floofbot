using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class LogConfig
    {
        [Key]
        public ulong ServerId { get; set; }
        public ulong MessageUpdatedChannel { get; set; }
        public ulong MessageDeletedChannel { get; set; }
        public ulong UserBannedChannel { get; set; }
        public ulong UserUnbannedChannel { get; set; }
        public ulong UserJoinedChannel { get; set; }
        public ulong UserLeftChannel { get; set; }
        public ulong MemberUpdatesChannel { get; set; }
        public ulong UserKickedChannel { get; set; }
        public ulong UserMutedChannel { get; set; }
        public ulong UserUnmutedChannel { get; set; }
        public bool IsOn { get; set; }
    }
}
