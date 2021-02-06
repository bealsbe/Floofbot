using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Security.Policy;

namespace Floofbot.Services.Repository.Models
{
    public partial class UserRolesList
    {
        [Key]
        public int Id { get; set; }
        public ulong UserID { get; set; }
        public ulong ServerId { get; set; }
        public string ListOfRoleIds { get; set; }
        public DateTime UTCTimestamp { get; set; }
    }
}
