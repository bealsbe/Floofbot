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
        private WelcomeGate GetServerConfig(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.WelcomeGateConfigs.Find(server);
            if (serverConfig == null)
            {
                _floofDB.Add(new WelcomeGate
                {
                    GuildID = server,
                    Toggle = false,
                    RoleId = null
                }) ;
                _floofDB.SaveChanges();
                return _floofDB.WelcomeGateConfigs.Find(server);
            }
            else
            {
                return serverConfig;
            }
        }
        private Discord.Color GenerateColor()
        {
            return new Discord.Color((uint)new Random().Next(0x1000000));
        }

        [Command("toggle")]
        [Summary("Toggles the welcome gate role assignment")]
        public async Task Toggle()
        {

            // try toggling
            try
            {
                // check the status of logger
                var ServerConfig = GetServerConfig(Context.Guild.Id);

                ServerConfig.Toggle = !ServerConfig.Toggle;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Welcome gate role assignment " + (ServerConfig.Toggle ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle welcome gate role assignment: " + ex);
                return;
            }
        }
        [Command("role")]
        [Summary("The role to add to the user")]
        public async Task SetRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `welcomegateconfig [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var ServerConfig = GetServerConfig(Context.Guild.Id);
            bool roleFound = false;
            ulong? roleId = null;
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == roleName.ToLower())
                    {
                        if (roleFound == false) // ok we found 1 role thats GOOD
                        {
                            roleFound = true;
                            roleId = r.Id;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return;
                        }
                    }
                }

                if (roleFound)
                {
                    ServerConfig.RoleId = roleId;
                    _floofDB.SaveChanges();
                    await Context.Channel.SendMessageAsync("Role set!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Unable to find that role. Role not set.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to set the welcome gate role: " + ex);
            }
        }
    }

}
