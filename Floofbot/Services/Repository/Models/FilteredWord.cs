using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class FilteredWord
    {
        [Key]
        public ulong Id { get; set; }
        public ulong ServerId { get; set; }
        public string Word { get; set; }
    }
}
