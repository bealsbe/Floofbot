using System;
using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class Warning
    {
        [Key]
        public ulong Id { get; set; }
        public DateTime DateAdded { get; set; }
        public bool Forgiven { get; set; }
        public ulong ForgivenBy { get; set; }
        public ulong GuildId { get; set; }
        public ulong Moderator { get; set; }
        public string Reason { get; set; }
        public ulong UserId { get; set; }
    }
}
