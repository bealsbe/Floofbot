using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using Floofbot.Services.Repository;
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

        private Color GenerateColor()
        {
            return new Color((uint)new Random().Next(0x1000000));
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
            // Checks if a word exists in the filter db
            var isRoleInDb = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == guild.Id).Any(r => r.RoleId == role.Id);
            
            return isRoleInDb;
        }

        [Summary("Join a role")]
        [Command("iam")]
        public async Task IAm([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder { Description = $"💾 Usage: `iam [rolename]`", Color = GenerateColor() }.Build());
                
                return;
            }

            // Find the role by iterating over guild roles
            var role = GetRole(roleName.ToLower(), Context.Guild);
            
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name.");
                return;
            }

            // Check that they are actually allowed to join the role
            var roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            
            if (roleExistsInDb == false)
            {
                await Context.Channel.SendMessageAsync("You cannot join that role.");
                return;
            }

            var usr = Context.User as IGuildUser;
            
            foreach (ulong r in usr.RoleIds)
            {
                if (r != role.Id) continue;
                
                await Context.Channel.SendMessageAsync("You already have that role.");
                return;
            }
            
            // Try to add the role onto the user
            try
            {
                await usr.AddRoleAsync(role, new RequestOptions { AuditLogReason = "User assigned themselves the role" });
                await Context.Channel.SendMessageAsync($"You now have the '{role.Name}' role.");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("Unable to give you that role. I may not have the permissions.");
                Log.Error("Error trying to add a role onto a user: " + ex);
            }
        }

        [Summary("Remove a role")]
        [Command("iamnot")]
        public async Task IAmNot([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder { Description = $"💾 Usage: `iamnot [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            // Find the role by iterating over guild roles
            var role = GetRole(roleName.ToLower(), Context.Guild);
            
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name.");
                return;
            }

            // check that the role exists in the database as a joinable roll
            var roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            
            if (roleExistsInDb == false)
            {
                await Context.Channel.SendMessageAsync("You cannot leave that role.");
                return;
            }

            // if they have the role we remove it
            var usr = Context.User as IGuildUser;
            
            foreach (ulong r in usr.RoleIds)
            {
                if (r != role.Id) continue;
                
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
            
            // User does not have role
            await Context.Channel.SendMessageAsync("You do not have that role.");
        }
    }
}
