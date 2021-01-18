using Discord;
﻿using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Nickname alert configuration commands")]
    [Name("NicknameAlert")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("nicknamealert")]
    public class NicknameAlert : ModuleBase<SocketCommandContext>
    {
        private FloofDataContext _floofDB;

        public NicknameAlert(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }

        private void CheckServerEntryExists(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.NicknameAlertConfigs.Find(server);
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
            CheckServerEntryExists(Context.Guild.Id);
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
                CheckServerEntryExists(Context.Guild.Id);
                // check the status of logger
                var ServerConfig = _floofDB.NicknameAlertConfigs.Find(Context.Guild.Id);
                ServerConfig.IsOn = !ServerConfig.IsOn;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Nickname Alerts " + (ServerConfig.IsOn ? "Enabled!" : "Disabled!"));
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
