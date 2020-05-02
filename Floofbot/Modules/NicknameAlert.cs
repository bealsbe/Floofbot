using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Group("nicknamealert")]
    public class NicknameAlert : ModuleBase<SocketCommandContext>
    {
        private FloofDataContext _floofDB;

        public NicknameAlert(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }

        protected void CheckServer(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.LogConfigs.Find(server);
            if (serverConfig == null)
            {
                _floofDB.Add(new NicknameAlertConfig
                {
                    ServerId = server,
                    Channel = 0,
                    IsOn = false
                }) ;
                _floofDB.SaveChanges();
            }
        }

        [Command("setchannel")] // update into a group
        [Summary("Sets the channel for the nickname alerts")]
        public async Task Channel([Summary("Channel (eg #alerts)")]Discord.IChannel channel)
        {
            var ServerConfig = _floofDB.NicknameAlertConfigs.Find(Context.Guild.Id);
            ServerConfig.Channel = channel.Id;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Channel updated! I will send nickname alerts to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the nickname alerts")]
        public async Task Toggle()
        {

            // try toggling
            try
            {
                // check the status of logger
                var ServerConfig = _floofDB.NicknameAlertConfigs.Find(Context.Guild.Id);

                bool bEnabled = ServerConfig.IsOn;
                if (!bEnabled)
                {
                    ServerConfig.IsOn = true;
                    await Context.Channel.SendMessageAsync("Nicknqme Alerts Enabled!");
                }
                else if (bEnabled)
                {
                    ServerConfig.IsOn = false;
                    await Context.Channel.SendMessageAsync("Nickname Alerts Disabled!");
                }
                _floofDB.SaveChanges();
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle the nickname alerts: " + ex);
                return;
            }
        }

    }
}
