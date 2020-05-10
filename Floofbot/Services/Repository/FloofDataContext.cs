using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;

namespace Floofbot.Services.Repository
{
    public partial class FloofDataContext : DbContext
    {
        public FloofDataContext()
        {
        }

        public FloofDataContext(DbContextOptions<FloofDataContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LogConfig> LogConfigs { get; set; }
        public virtual DbSet<Tag> Tags { get; set; }
        public virtual DbSet<Warning> Warnings { get; set; }
        public virtual DbSet<AdminConfig> AdminConfig { get; set; }
        public virtual DbSet<NicknameAlertConfig> NicknameAlertConfigs { get; set; }
        public virtual DbSet<FilterConfig> FilterConfigs { get; set; }
        public virtual DbSet<FilteredWord> FilteredWords { get; set; }
        public virtual DbSet<FilterChannelWhitelist> FilterChannelWhitelists { get; set; }
        public virtual DbSet<UserAssignableRole> UserAssignableRoles { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("DataSource=floofData.db");
            }
        }
    }
}
