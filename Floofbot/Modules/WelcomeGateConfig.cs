using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Name("Welcome Gate Configuration")]
    [Summary("Configures auto-adding roles when users bypass the welcome gate")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("welcomegateconfig")]
    public class WelcomeGateConfig : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public WelcomeGateConfig(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        
        private async Task<WelcomeGate> GetServerConfigAsync(ulong server)
        {
            // Checks if server exists in database and adds if not
            var serverConfig = _floofDB.WelcomeGateConfigs.Find(server);

            if (serverConfig != null) return serverConfig;
            
            _floofDB.Add(new WelcomeGate
            {
                GuildID = server,
                Toggle = false,
                RoleId = null
            }) ;
                
            await _floofDB.SaveChangesAsync();
                
            return await _floofDB.WelcomeGateConfigs.FindAsync(server);

        }
        
        private Color GenerateColor()
        {
            return new Color((uint)new Random().Next(0x1000000));
        }

        [Command("toggle")]
        [Summary("Toggles the welcome gate role assignment")]
        public async Task Toggle()
        {
            // Try toggling
            try
            {
                // Check the status of logger
                var serverConfig = await GetServerConfigAsync(Context.Guild.Id);

                serverConfig.Toggle = !serverConfig.Toggle;
                
                await _floofDB.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync("Welcome gate role assignment " + (serverConfig.Toggle ? "Enabled!" : "Disabled!"));
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + e.Message);
                Log.Error("Error when trying to toggle welcome gate role assignment: " + e);
            }
        }
        
        [Command("role")]
        [Summary("The role to add to the user")]
        public async Task SetRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder { Description = $"💾 Usage: `welcomegateconfig [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var serverConfig = await GetServerConfigAsync(Context.Guild.Id);
            var roleFound = false;
            ulong? roleId = null;
            
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() != roleName.ToLower()) continue;
                    
                    if (roleFound == false) // Ok we found 1 role thats GOOD
                    {
                        roleFound = true;
                        roleId = r.Id;
                    }
                    else // There is more than 1 role with the same name!
                    {
                        await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                        return;
                    }
                }

                if (roleFound)
                {
                    serverConfig.RoleId = roleId;
                    
                    await _floofDB.SaveChangesAsync();
                    
                    await Context.Channel.SendMessageAsync("Role set!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Unable to find that role. Role not set.");
                }
            }
            catch (Exception e)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + e.Message);
                Log.Error("Error when trying to set the welcome gate role: " + e);
            }
        }
    }

}
