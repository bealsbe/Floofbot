using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public partial class NicknameAlertConfig
    {
        [Key]
        public long Id { get; set; }
        public ulong ServerId { get; set; }
        public ulong Channel { get; set; }
        public bool IsOn { get; set; }
    }
}
