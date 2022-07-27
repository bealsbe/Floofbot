using System;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;

namespace Floofbot.Modules
{
    [Summary("Modmail configuration commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Name("ModMail Config")]
    [Group("modmailconfig")]
    public class ModMailConfigModule : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public ModMailConfigModule(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        private Color GenerateColor()
        {
            return new Color((uint)new Random().Next(0x1000000));
        }

        private async Task<ModMail> GetServerConfigAsync(ulong server)
        {
            // Checks if server exists in database and adds if not
            var serverConfig = _floofDB.ModMails.Find(server);

            if (serverConfig != null) return serverConfig;
            
            _floofDB.Add(new ModMail
            {
                ServerId = server,
                ChannelId = null,
                IsEnabled = false,
                ModRoleId = null
            });
                
            await _floofDB.SaveChangesAsync();
                
            return await _floofDB.ModMails.FindAsync(server);
        }

        [Command("channel")]
        [Summary("Sets the channel for the modmail notifications")]
        public async Task Channel([Summary("Channel (eg #alerts)")]IChannel channel = null)
        {
            // If channel is null we assign the Context.Channel
            channel ??= (IChannel) Context.Channel;
            
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
            serverConfig.ChannelId = channel.Id;
            
            await _floofDB.SaveChangesAsync();
            
            await Context.Channel.SendMessageAsync("Channel updated! I will send modmails to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the modmail module")]
        public async Task Toggle()
        {
            // Try toggling
            try
            {
                // Check the status of logger
                var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
                
                if (serverConfig.ChannelId == null)
                {
                    await Context.Channel.SendMessageAsync("Channel not set! Please set the channel before toggling the ModMail feature.");
                    return;
                }
                
                serverConfig.IsEnabled = !serverConfig.IsEnabled;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Modmail " + (serverConfig.IsEnabled ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                
                Log.Error("Error when trying to toggle the modmail: " + ex);
            }
        }
        [Command("modrole")]
        [Summary("OPTIONAL: A Role to Ping When ModMail is Received.")]
        public async Task SetModRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `modmailconfig [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
            var modRoleFound = false;
            ulong? roleId = null;
            
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() != roleName.ToLower()) continue;
                    
                    if (modRoleFound == false) // OK, we found 1 role that's GOOD
                    {
                        modRoleFound = true;
                        roleId = r.Id;
                    }
                    else // There is more than 1 role with the same name!
                    {
                        await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                        return;
                    }
                }

                if (modRoleFound)
                {
                    serverConfig.ModRoleId = roleId;
                    
                    await _floofDB.SaveChangesAsync();
                    
                    await Context.Channel.SendMessageAsync("Mod role set!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Unable to find that role. Role not set.");
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                
                Log.Error("Error when trying to set the modmail mod role: " + ex);
            }
        }
    }
}