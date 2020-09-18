using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class RaidProtectionConfig
    {
        [Key]
        public ulong ServerId { get; set; }
        public bool Enabled { get; set; }
        public ulong? ModChannelId { get; set; }
        public ulong? ModRoleId { get; set; }
        public ulong? MutedRoleId { get; set; }
        public ulong? ExceptionRoleId { get; set; }
        public bool BanOffenders { get; set; }
    }
}

