using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class FilterChannelWhitelist
    {
        [Key]
        public ulong ChannelId { get; set; }
        public ulong ServerId { get; set; }
    }
}

