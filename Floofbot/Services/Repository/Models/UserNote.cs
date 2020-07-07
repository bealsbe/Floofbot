using System;
using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class UserNote
    {
        [Key]
        public ulong Id { get; set; }
        public DateTime DateAdded { get; set; }
        public bool Forgiven { get; set; }
        public ulong ForgivenBy { get; set; }
        public ulong GuildId { get; set; }
        public ulong ModeratorId { get; set; }
        public string Moderator { get; set; }
        public string Reason { get; set; }
        public ulong UserId { get; set; }
    }
}

