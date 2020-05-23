using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class FilterConfig
    {
        [Key]
        public long Id { get; set; }
        public ulong ServerId { get; set; }
        public bool IsOn { get; set; }
    }
}
