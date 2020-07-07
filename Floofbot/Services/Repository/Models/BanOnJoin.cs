using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Floofbot.Services.Repository.Models
{
    public partial class BanOnJoin
    {
        [Key]
        public int Id { get; set; }
        public ulong UserID { get; set; }
        public ulong ModID { get; set; }
        public string ModUsername { get; set; }
        public string Reason { get; set; }
    }
}
