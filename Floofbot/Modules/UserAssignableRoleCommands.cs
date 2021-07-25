using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System.Linq;
using Discord.Addons.Interactive;
using Discord.WebSocket;
using Serilog;

namespace Floofbot.Modules
{
    [RequireContext(ContextType.Guild)]
    [Summary("Add/Remove allowable roles for yourself")]
    [Name("User Assignable Roles")]
    public class UserAssignableRoleCommands : InteractiveBase
    {
        private FloofDataContext _floofDb;
        public UserAssignableRoleCommands(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }

        private Discord.Color GenerateColor()
        {
            return new Discord.Color((uint)new Random().Next(0x1000000));
        }

        private SocketRole GetRole(string roleName, SocketGuild guild)
        {
            foreach (SocketRole r in guild.Roles)
            {
                if (r.Name.ToLower() == roleName)
                    return r;
            }
            return null;
        }
        private bool CheckRoleEntryExists(SocketRole role, SocketGuild guild)
        {
            // checks if a word exists in the filter db
            bool isRoleInDb = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == guild.Id).Where(r => r.RoleId == role.Id).Any();
            return isRoleInDb;
        }

        [Summary("Join a role")]
        [Command("iam")]
        public async Task IAm([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `iam [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            // find the role by iterating over guild roles
            SocketRole role = GetRole(roleName.ToLower(), Context.Guild);
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name.");
                return;
            }

            // check that they are actually allowed to join the role
            bool roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            if (roleExistsInDb == false)
            {
                await Context.Channel.SendMessageAsync("You cannot join that role.");
                return;
            }

            IGuildUser usr = Context.User as IGuildUser;
            foreach (ulong r in usr.RoleIds)
            {
                if (r == role.Id)
                {
                    await Context.Channel.SendMessageAsync("You already have that role.");
                    return;
                }
            }
            // try to add the role onto the user
            try
            {
                await usr.AddRoleAsync(role, new RequestOptions { AuditLogReason = "User assigned themselves the role" });
                await Context.Channel.SendMessageAsync($"You now have the '{role.Name}' role.");
                return;
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("Unable to give you that role. I may not have the permissions.");
                Log.Error("Error trying to add a role onto a user: " + ex);
                return;
            }

        }

        [Summary("Remove a role")]
        [Command("iamnot")]
        public async Task IAmNot([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `iamnot [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            // find the role by iterating over guild roles
            SocketRole role = GetRole(roleName.ToLower(), Context.Guild);
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name.");
                return;
            }

            // check that the role exists in the database as a joinable roll
            bool roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            if (roleExistsInDb == false)
            {
                await Context.Channel.SendMessageAsync("You cannot leave that role.");
                return;
            }

            // if they have the role we remove it
            IGuildUser usr = Context.User as IGuildUser;
            foreach (ulong r in usr.RoleIds)
            {
                if (r == role.Id)
                {
                    try
                    {
                        await usr.RemoveRoleAsync(role, new RequestOptions { AuditLogReason = "User removed the role themselves." });
                        await Context.Channel.SendMessageAsync($"You no longer have the '{role.Name}' role.");
                        return;
                    }
                    catch (Exception ex)
                    {
                        await Context.Channel.SendMessageAsync("Unable to remove that role. I may not have the permissions.");
                        Log.Error("Error trying to remove a role from a user: " + ex);
                        return;
                    }
                }
            }
            // user does not have role
            await Context.Channel.SendMessageAsync("You do not have that role.");
        }
    }

    [Summary("Configures the joinable user roles")]
    [Name("User Assignable Roles Configuration")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("iamconfig")]
    public class UserAssignableRoleAdminCommands : InteractiveBase
    {
        FloofDataContext _floofDb;
        private readonly Discord.Color EMBED_COLOUR = new Discord.Color((uint)new Random().Next(0x1000000));
        public UserAssignableRoleAdminCommands(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }
        private SocketRole GetRole(string roleName, SocketGuild guild)
        {
            foreach (SocketRole r in guild.Roles)
            {
                if (r.Name.ToLower() == roleName)
                    return r;
            }
            return null;
        }
        private bool CheckRoleEntryExists(SocketRole role, SocketGuild guild)
        {
            // checks if a word exists in the filter db
            bool isRoleInDb = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == guild.Id).Where(r => r.RoleId == role.Id).Any();
            return isRoleInDb;
        }

        [Summary("Configure a role as joinable")]
        [Command("add")]
        public async Task AddRole([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `iamconfig add [rolename]`", Color = EMBED_COLOUR }.Build());
                return;
            }

            // find the role by iterating over guild roles
            SocketRole role = GetRole(roleName.ToLower(), Context.Guild);
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name to add.");
                return;
            }

            bool roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            if (roleExistsInDb == true)
            {
                await Context.Channel.SendMessageAsync("Users can already join that role.");
                return;
            }
            else
            {
                try
                {
                    _floofDb.Add(new UserAssignableRole
                    {
                        RoleId = role.Id,
                        ServerId = Context.Guild.Id
                    });
                    _floofDb.SaveChanges();
                    await Context.Channel.SendMessageAsync($"{role.Name} ({role.Id}) can now be joined by users!");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured when trying to add that role.");
                    Log.Error("Error when trying to add a joinable role: " + ex);
                }
            }
        }

        [Summary("Configure a role to no longer be joinable")]
        [Command("remove")]
        public async Task RemoveRole([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `iamconfig remove [rolename]`", Color = EMBED_COLOUR }.Build());
                return;
            }

            // find the role by iterating over guild roles
            SocketRole role = GetRole(roleName.ToLower(), Context.Guild);
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name to remove.");
                return;
            }

            bool roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            if (roleExistsInDb == false)
            {
                await Context.Channel.SendMessageAsync("Users already cannot join that role.");
                return;
            }
            else
            {
                try
                {
                    UserAssignableRole roleDbEntry = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == Context.Guild.Id).Where(r => r.RoleId == role.Id).First();
                    _floofDb.Remove(roleDbEntry);
                    _floofDb.SaveChanges();
                    await Context.Channel.SendMessageAsync($"{role.Name} ({role.Id}) can no longer be joined!");
                }
                catch (Exception ex)
                {
                    await Context.Channel.SendMessageAsync("An error occured when trying to remove that role: " + ex.Message);
                    Log.Error("Error when trying to remove a joinable role " + ex);
                }
            }

        }
    }
}
