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
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("raidconfig")]
    public class RaidProtectionCommands : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public RaidProtectionCommands(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        private Discord.Color GenerateColor()
        {
            return new Discord.Color((uint)new Random().Next(0x1000000));
        }

        private RaidProtectionConfig GetServerConfig(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.RaidProtectionConfigs.Find(server);
            if (serverConfig == null)
            {
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
                _floofDB.SaveChanges();
                return _floofDB.RaidProtectionConfigs.Find(server);
            }
            else
            {
                return serverConfig;
            }
        }
        private async Task<SocketRole> resolveRole(string input, SocketGuild guild, ISocketMessageChannel channel)
        {
            SocketRole role = null;
            //resolve roleID or @mention
            if (Regex.IsMatch(input, @"\d{10,}"))
            {
                string roleID = Regex.Match(input, @"\d{10,}").Value;
                role = guild.GetRole(Convert.ToUInt64(roleID));
            }
            //resolve role name
            else
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == input.ToLower())
                    {
                        if (role == null) // ok we found 1 role thats GOOD
                        {
                            role = r;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return null;
                        }
                    }
                }
            }
             return role;
        }
        [Command("modchannel")]
        [Summary("OPTIONAL: Sets the mod channel for raid notifications")]
        public async Task ModChannel([Summary("Channel (eg #alerts)")]Discord.IChannel channel = null)
        {
            if (channel == null)
            {
                channel = Context.Channel;
            }
            var ServerConfig = GetServerConfig(Context.Guild.Id);
            ServerConfig.ModChannelId = channel.Id;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Channel updated! I will raid notifications to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the raid protection")]
        public async Task Toggle()
        {
            // check the status of raid protection
            var ServerConfig = GetServerConfig(Context.Guild.Id);
            ServerConfig.Enabled = !ServerConfig.Enabled;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Raid Protection " + (ServerConfig.Enabled ? "Enabled!" : "Disabled!"));
        }
        [Command("togglebans")]
        [Summary("Toggles whether to ban or mute users that trigger the raid detection.")]
        public async Task ToggleBans()
        {

            // try toggling
            try
            {
                // check the status of raid protection
                var ServerConfig = GetServerConfig(Context.Guild.Id);
                ServerConfig.BanOffenders = !ServerConfig.BanOffenders;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Raid Protection Bans " + (ServerConfig.BanOffenders ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle raid protection banning: " + ex);
                return;
            }
        }
        [Command("modrole")]
        [Summary("OPTIONAL: A role to ping for raid alerts.")]
        public async Task SetModRole(string role = null)
        {
            var ServerConfig = GetServerConfig(Context.Guild.Id);

            if (role == null)
            {
                ServerConfig.ModRoleId = null;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Mod role removed. I will no longer ping a mod role.");
                return;
            }

            var foundRole = await resolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                ServerConfig.ModRoleId = foundRole.Id;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Mod role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
                return;
            }
        }
        [Command("mutedrole")]
        [Summary("OPTIONAL: A role to give to users to mute them in the server. If not set, users are banned by default.")]
        public async Task SetMutedRole(string role = null)
        {
            var ServerConfig = GetServerConfig(Context.Guild.Id);

            if (role == null)
            {
                ServerConfig.MutedRoleId = null;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Muted role removed. I will now default to banning users.");
                return;
            }

            var foundRole = await resolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                ServerConfig.MutedRoleId = foundRole.Id;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Muted role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
                return;
            }
        }
        [Command("exceptionsrole")]
        [Summary("OPTIONAL: Users with this role are immune to raid protection.")]
        public async Task SetExceptionsRole(string role = null)
        {
            var ServerConfig = GetServerConfig(Context.Guild.Id);

            if (role == null)
            {
                ServerConfig.ExceptionRoleId = null;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Exceptions role removed. Users will no longer be exempt from raid protection.");
                return;
            }

            var foundRole = await resolveRole(role, Context.Guild, Context.Channel);

            if (foundRole != null)
            {
                ServerConfig.ExceptionRoleId = foundRole.Id;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Exceptions role set to " + foundRole.Name + "!");
            }
            else
            {
                await Context.Channel.SendMessageAsync("Unable to set that role.");
                return;
            }
        }
    }
}
