using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class WelcomeGate
    {
        [Key]
        public ulong GuildID { get; set; }
        public ulong? RoleId { get; set; }
        public bool Toggle { get; set; }
    }
}

