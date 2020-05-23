using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class AdminConfig
    {
        [Key]
        public long Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong MuteRoleId { get; set; }
    }
}
