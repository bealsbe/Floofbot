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

            // try toggling
            try
            {
                // check the status of raid protection
                var ServerConfig = GetServerConfig(Context.Guild.Id);
                ServerConfig.Enabled = !ServerConfig.Enabled;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Raid Protection " + (ServerConfig.Enabled ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle raid protection: " + ex);
                return;
            }
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
        public async Task SetModRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `raidconfig modrole [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var ServerConfig = GetServerConfig(Context.Guild.Id);
            bool modRoleFound = false;
            ulong? roleId = null;
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == roleName.ToLower())
                    {
                        if (modRoleFound == false) // ok we found 1 role thats GOOD
                        {
                            modRoleFound = true;
                            roleId = r.Id;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return;
                        }
                    }
                }

                if (modRoleFound)
                {
                    ServerConfig.ModRoleId = roleId;
                    _floofDB.SaveChanges();
                    await Context.Channel.SendMessageAsync("Mod role set!");
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
                Log.Error("Error when trying to set the raid protection mod role: " + ex);
            }
        }
        [Command("mutedrole")]
        [Summary("OPTIONAL: A role to give to users to mute them in the server. If not set, users are banned by default.")]
        public async Task SetMutedRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `raidconfig mutedrole [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var ServerConfig = GetServerConfig(Context.Guild.Id);
            bool mutedRoleFound = false;
            ulong? roleId = null;
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == roleName.ToLower())
                    {
                        if (mutedRoleFound == false) // ok we found 1 role thats GOOD
                        {
                            mutedRoleFound = true;
                            roleId = r.Id;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return;
                        }
                    }
                }

                if (mutedRoleFound)
                {
                    ServerConfig.MutedRoleId = roleId;
                    _floofDB.SaveChanges();
                    await Context.Channel.SendMessageAsync("Muted role set!");
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
                Log.Error("Error when trying to set the raid protection muted role: " + ex);
            }
        }
        [Command("exceptionsrole")]
        [Summary("OPTIONAL: Users with this role are immune to raid protection.")]
        public async Task SetExceptionsRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `raidconfig exceptionsrole [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var ServerConfig = GetServerConfig(Context.Guild.Id);
            bool exceptionsRoleFound = false;
            ulong? roleId = null;
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == roleName.ToLower())
                    {
                        if (exceptionsRoleFound == false) // ok we found 1 role thats GOOD
                        {
                            exceptionsRoleFound = true;
                            roleId = r.Id;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return;
                        }
                    }
                }

                if (exceptionsRoleFound)
                {
                    ServerConfig.ExceptionRoleId = roleId;
                    _floofDB.SaveChanges();
                    await Context.Channel.SendMessageAsync("Exceptions role set!");
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
                Log.Error("Error when trying to set the raid protection exceptions role: " + ex);
            }
        }
    }
}
