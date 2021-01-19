using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Floofbot.Services.Repository.Models
{
    public partial class BlacklistedUser
    {
        [Key]
        public ulong UserID { get; set; }
        public string BannedFromServers { get; set; }
        public bool IsGlobal { get; set; }
    }
}
