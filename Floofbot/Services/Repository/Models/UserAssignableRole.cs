using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class UserAssignableRole
    {
        [Key]
        public ulong RoleId { get; set; }
        public ulong ServerId { get; set; }
    }
}

