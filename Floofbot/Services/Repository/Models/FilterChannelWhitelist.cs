using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class FilterChannelWhitelist
    {
        [Key]
        public long Id { get; set; }
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
    }
}

