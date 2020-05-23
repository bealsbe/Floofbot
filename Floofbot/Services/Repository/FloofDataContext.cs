using Microsoft.EntityFrameworkCore;
using Floofbot.Configs;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;

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
        public virtual DbSet<TagConfig> TagConfigs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                try
                {
                    BotDbConnection connection = BotConfigFactory.Config.DbConnection;
                    string connectionString = $"Server={connection.ServerIP};" +
                        $"Port={connection.Port};" +
                        $"Database={connection.DatabaseName};" +
                        $"User Id={connection.Username};" +
                        $"Password={connection.Password};";
                    optionsBuilder.UseNpgsql(connectionString);
                }
                catch (Exception e)
                {
                    Log.Error("Issue occurred with initializing DB connection string. Is app.config set up correctly? " + e);
                    Environment.Exit(1);
                }
            }
        }
    }
}
