using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Floofbot.Services.Repository.Models
{
    public class Tag
    {
        [Key]
        public ulong TagId { get; set; }
        public ulong UserId { get; set; }
        public string Content { get; set; }
    }
}
