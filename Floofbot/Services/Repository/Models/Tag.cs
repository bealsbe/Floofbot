using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class Tag
    {
        [Key]
        public long TagId { get; set; }
        public string TagName { get; set; }
        public ulong ServerId { get; set; }
        public ulong UserId { get; set; }
        public string TagContent { get; set; }
    }
}
