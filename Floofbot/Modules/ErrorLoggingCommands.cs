using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Threading.Tasks;

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
        // checks if server exists in database and adds if not
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
        else
        {
            return serverConfig;
        }
    }

    [Command("channel")]
    [Summary("Sets the channel for logging fatal errors")]
    public async Task Channel([Summary("Channel (eg #errors)")]Discord.IChannel channel = null)
    {
        if (channel == null)
        {
            channel = (IChannel)Context.Channel;
        }
        var ServerConfig = GetServerConfig(Context.Guild.Id);
        ServerConfig.ChannelId = channel.Id;
        _floofDB.SaveChanges();
        await Context.Channel.SendMessageAsync("Channel updated! I will send fatal errors to <#" + channel.Id + ">");
    }

    [Command("toggle")]
    [Summary("Toggles error logging")]
    public async Task Toggle()
    {

        // try toggling
            // check the status of logger
            var ServerConfig = GetServerConfig(Context.Guild.Id);
            if (ServerConfig.ChannelId == null)
            {
                await Context.Channel.SendMessageAsync("Channel not set! Please set the channel before toggling error logging.");
                return;
            }
            ServerConfig.IsOn = !ServerConfig.IsOn;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Error Logging " + (ServerConfig.IsOn ? "Enabled!" : "Disabled!"));
    }
}