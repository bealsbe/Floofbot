using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;

namespace Floofbot.Modules
{
    [Summary("Error logging configuration commands")]
    [Name("Error Logging Configuration Commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("errorloggingconfig")]
    public class ErrorLoggingCommands : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public ErrorLoggingCommands(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }

        private ErrorLogging GetServerConfig(ulong server)
        {
            // Checks if server exists in the database and adds if is not
            var serverConfig = _floofDB.ErrorLoggingConfigs.Find(server);
        
            if (serverConfig == null)
            {
                _floofDB.Add(new ErrorLogging
                {
                    ServerId = server,
                    ChannelId = null,
                    IsOn = false
                });
            
                _floofDB.SaveChanges();
            
                return _floofDB.ErrorLoggingConfigs.Find(server);
            }

            return serverConfig;
        }

        [Command("channel")]
        [Summary("Sets the channel for logging fatal errors")]
        public async Task Channel([Summary("Channel (eg #errors)")]Discord.IChannel channel = null)
        {
            if (channel == null)
            {
                channel = (IChannel)Context.Channel;
            }
        
            var serverConfig = GetServerConfig(Context.Guild.Id);
        
            serverConfig.ChannelId = channel.Id;
        
            await _floofDB.SaveChangesAsync();
        
            await Context.Channel.SendMessageAsync("Channel updated! I will send fatal errors to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles error logging")]
        public async Task Toggle()
        {
            // Try toggling
            // Check the status of logger
            var serverConfig = GetServerConfig(Context.Guild.Id);
            
            if (serverConfig.ChannelId == null)
            {
                await Context.Channel.SendMessageAsync("Channel not set! Please set the channel before toggling error logging.");
                
                return;
            }
            
            serverConfig.IsOn = !serverConfig.IsOn;
            
            await _floofDB.SaveChangesAsync();
            
            await Context.Channel.SendMessageAsync("Error Logging " + (serverConfig.IsOn ? "Enabled!" : "Disabled!"));
        }
    }
}