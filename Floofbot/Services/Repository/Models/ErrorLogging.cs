using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class ErrorLogging
    {
        [Key]
        public ulong ServerId { get; set; }
        public ulong? ChannelId { get; set; }
        public bool IsOn { get; set; }
    }
}

