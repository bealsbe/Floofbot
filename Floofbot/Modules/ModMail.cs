using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Send a message directly to the server's moderators")]
    [Group("modmail")]
    public class ModMailModule : InteractiveBase
    {
        private FloofDataContext _floofDb;
        static readonly ulong RFURRY_SERVER_ID = 225980129799700481; // TODO: Replace this so that it works on more servers
        public ModMailModule(FloofDataContext _floofDB)
        {
            _floofDb = _floofDB;
        }

        [Command("")]
        public async Task sendModMail([Summary("Message Content")][Remainder] string content)
        {

        }
    }
    [Summary("Modmail configuration commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("modmailconfig")]
    public class ModMailConfigModule : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public ModMailConfigModule(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        private Discord.Color GenerateColor()
        {
            return new Discord.Color((uint)new Random().Next(0x1000000));
        }

        private void CheckServerEntryExists(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.ModMails.Find(server);
            if (serverConfig == null)
            {
                _floofDB.Add(new ModMail
                {
                    ServerId = server,
                    ChannelId = null,
                    IsEnabled = false,
                    ModRoleId = null
                });
                _floofDB.SaveChanges();
            }
        }

        [Command("channel")]
        [Summary("Sets the channel for the modmail notifications")]
        public async Task Channel([Summary("Channel (eg #alerts)")]Discord.IChannel channel)
        {
            CheckServerEntryExists(Context.Guild.Id);
            var ServerConfig = _floofDB.ModMails.Find(Context.Guild.Id);
            ServerConfig.ChannelId = channel.Id;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Channel updated! I will send modmails to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the modmail module")]
        public async Task Toggle()
        {

            // try toggling
            try
            {
                CheckServerEntryExists(Context.Guild.Id);
                // check the status of logger
                var ServerConfig = _floofDB.ModMails.Find(Context.Guild.Id);
                if (ServerConfig.ChannelId == null)
                {
                    await Context.Channel.SendMessageAsync("Channel not set! Please set the channel before toggling the ModMail feature.");
                    return;
                }
                ServerConfig.IsEnabled = !ServerConfig.IsEnabled;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Modmail " + (ServerConfig.IsEnabled ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle the modmail: " + ex);
                return;
            }
        }
        [Command("modrole")]
        [Summary("OPTIONAL: A Role to Ping When ModMail is Received.")]
        public async Task SetModRole(string roleName = null)
        {
            var ServerConfig = _floofDB.ModMails.Find(Context.Guild.Id);
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `modmailconfig [rolename]`", Color = GenerateColor() }.Build());
                return;
            }
            foreach (SocketRole r in Context.Guild.Roles)
            {
                if (r.Name.ToLower() == roleName.ToLower())
                {
                    try
                    {
                        ServerConfig.ModRoleId = r.Id;
                        await Context.Channel.SendMessageAsync("Mod role set!");
                        _floofDB.SaveChanges();
                        return;
                    }
                    catch (Exception ex)
                    {
                        await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                        Log.Error("Error when trying to set the modmail mod role: " + ex);
                        return;
                    }
                }
            }
            await Context.Channel.SendMessageAsync("Unable to find that role. Role not set.");
        }
    }

}
