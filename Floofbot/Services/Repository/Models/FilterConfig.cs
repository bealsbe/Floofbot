using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class FilterConfig
    {
        [Key]
        public ulong ServerId { get; set; }
        public bool IsOn { get; set; }
    }
}
