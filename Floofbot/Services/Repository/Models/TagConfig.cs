using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class TagConfig
    {
        [Key]
        public long Id { get; set; }
        public ulong ServerId { get; set; }
        public bool TagUpdateRequiresAdmin { get; set; }
    }
}
