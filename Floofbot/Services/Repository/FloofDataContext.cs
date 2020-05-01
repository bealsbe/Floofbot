using System;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
        public virtual DbSet<NicknameAlert> NicknameAlerts { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("DataSource=floofData.db");
            }
        }
    }
}
