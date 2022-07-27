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

        private async Task CheckServerEntryExistsAsync(ulong server)
        {
            // Checks if server exists in database and adds if not
            var serverConfig = _floofDB.NicknameAlertConfigs.Find(server);

            if (serverConfig != null) return;
            
            _floofDB.Add(new NicknameAlertConfig
            {
                ServerId = server,
                Channel = 0,
                IsOn = false
            });
            
            await _floofDB.SaveChangesAsync();
        }

        [Command("setchannel")] // update into a group
        [Summary("Sets the channel for the nickname alerts")]
        public async Task Channel([Summary("Channel (eg #alerts)")]Discord.IChannel channel)
        {
            await CheckServerEntryExistsAsync(Context.Guild.Id);
            
            var serverConfig = _floofDB.NicknameAlertConfigs.Find(Context.Guild.Id);
            serverConfig.Channel = channel.Id;
            
            await _floofDB.SaveChangesAsync();
            
            await Context.Channel.SendMessageAsync("Channel updated! I will send nickname alerts to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the nickname alerts")]
        public async Task Toggle()
        {
            // Try toggling
            try
            {
                await CheckServerEntryExistsAsync(Context.Guild.Id);
                
                // Check the status of logger
                var serverConfig = _floofDB.NicknameAlertConfigs.Find(Context.Guild.Id);
                serverConfig.IsOn = !serverConfig.IsOn;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Nickname Alerts " + (serverConfig.IsOn ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle the nickname alerts: " + ex);
            }
        }
    }
}
