using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public class NicknameAlert
    {
        [Key]
        public ulong ServerId { get; set; }
        public ulong Channel { get; set; }
        public bool IsOn { get; set; }
    }
}
