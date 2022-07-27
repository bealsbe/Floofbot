using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Raid protection configuration commands")]
    [Name("Raid Protection Configuration Commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("raidconfig")]
    public class RaidProtectionCommands : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public RaidProtectionCommands(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        
        private Color GenerateColor()
        {
            return new Color((uint)new Random().Next(0x1000000));
        }

        private async Task<RaidProtectionConfig> GetServerConfigAsync(ulong server)
        {
            // Checks if server exists in database and adds if it is not
            var serverConfig = _floofDB.RaidProtectionConfigs.Find(server);

            if (serverConfig != null) return serverConfig;
            
            _floofDB.Add(new RaidProtectionConfig
            {
                ServerId = server,
                Enabled = false,
                ModChannelId = null,
                ModRoleId = null,
                MutedRoleId = null,
                BanOffenders = true,
                ExceptionRoleId = null
            });
                
            await _floofDB.SaveChangesAsync();
                
            return await _floofDB.RaidProtectionConfigs.FindAsync(server);
        }
        
        private async Task<SocketRole> ResolveRole(string input, SocketGuild guild, ISocketMessageChannel channel)
        {
            SocketRole role = null;
            
            // Resolve roleID or @mention
            if (Regex.IsMatch(input, @"\d{10,}"))
            {
                var roleID = Regex.Match(input, @"\d{10,}").Value;
                
                role = guild.GetRole(Convert.ToUInt64(roleID));
            }
            else  // Resolve role name
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() != input.ToLower()) continue;
                    
                    if (role == null) // OK, we found 1 role that's GOOD
                    {
                        role = r;
                    }
                    else // There is more than 1 role with the same name!
                    {
                        await channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                        return null;
                    }
                }
            }
            
            return role;
        }
        
        [Command("modchannel")]
        [Summary("OPTIONAL: Sets the mod channel for raid notifications")]
        public async Task ModChannel([Summary("Channel (eg #alerts)")]IChannel channel = null)
        {
            channel ??= Context.Channel;
            
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
            serverConfig.ModChannelId = channel.Id;
            
            await _floofDB.SaveChangesAsync();
            
            await Context.Channel.SendMessageAsync("Channel updated! I will raid notifications to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the raid protection")]
        public async Task Toggle()
        {
            // check the status of raid protection
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
            serverConfig.Enabled = !serverConfig.Enabled;
            
            await _floofDB.SaveChangesAsync();
            
            await Context.Channel.SendMessageAsync("Raid Protection " + (serverConfig.Enabled ? "Enabled!" : "Disabled!"));
        }
        
        [Command("togglebans")]
        [Summary("Toggles whether to ban or mute users that trigger the raid detection.")]
        public async Task ToggleBans()
        {
            // Try toggling
            try
            {
                // Check the status of raid protection
                var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
                serverConfig.BanOffenders = !serverConfig.BanOffenders;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Raid Protection Bans " + (serverConfig.BanOffenders ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle raid protection banning: " + ex);
            }
        }
        
        [Command("modrole")]
        [Summary("OPTIONAL: A role to ping for raid alerts.")]
        public async Task SetModRole(string role = null)
        {
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);

            if (role == null)
            {
                serverConfig.ModRoleId = null;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Mod role removed. I will no longer ping a mod role.");
                
                return;
            }

            var foundRole = await ResolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                serverConfig.ModRoleId = foundRole.Id;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Mod role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
            }
        }
        [Command("mutedrole")]
        [Summary("OPTIONAL: A role to give to users to mute them in the server. If not set, users are banned by default.")]
        public async Task SetMutedRole(string role = null)
        {
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);

            if (role == null)
            {
                serverConfig.MutedRoleId = null;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Muted role removed. I will now default to banning users.");
                
                return;
            }

            var foundRole = await ResolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                serverConfig.MutedRoleId = foundRole.Id;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Muted role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
            }
        }
        
        [Command("exceptionsrole")]
        [Summary("OPTIONAL: Users with this role are immune to raid protection.")]
        public async Task SetExceptionsRole(string role = null)
        {
            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);

            if (role == null)
            {
                serverConfig.ExceptionRoleId = null;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Exceptions role removed. Users will no longer be exempt from raid protection.");
                
                return;
            }

            var foundRole = await ResolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                serverConfig.ExceptionRoleId = foundRole.Id;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Exceptions role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
            }
        }
    }
}
