using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class ModMail
    {
        [Key]
        public ulong ServerId { get; set; }
        public bool IsEnabled { get; set; }
        public ulong? ModRoleId { get; set; }
        public ulong? ChannelId { get; set; }
    }
}