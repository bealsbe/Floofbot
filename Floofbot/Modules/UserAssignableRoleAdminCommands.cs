using System;
using System.Linq;
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
    [Summary("Configures the joinable user roles")]
    [Name("User Assignable Roles Configuration")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("iamconfig")]
    public class UserAssignableRoleAdminCommands : InteractiveBase
    {
        FloofDataContext _floofDb;
        private readonly Color EMBED_COLOUR = new Color((uint)new Random().Next(0x1000000));
        
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
            var isRoleInDb = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == guild.Id).Any(r => r.RoleId == role.Id);
            
            return isRoleInDb;
        }

        [Summary("Configure a role as joinable")]
        [Command("add")]
        public async Task AddRole([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder { Description = $"💾 Usage: `iamconfig add [rolename]`", Color = EMBED_COLOUR }.Build());
                return;
            }

            // Find the role by iterating over guild roles
            var role = GetRole(roleName.ToLower(), Context.Guild);
            
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name to add.");
                return;
            }

            var roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            
            if (roleExistsInDb)
            {
                await Context.Channel.SendMessageAsync("Users can already join that role.");
                return;
            }

            try
            {
                _floofDb.Add(new UserAssignableRole
                {
                    RoleId = role.Id,
                    ServerId = Context.Guild.Id
                });
                
                await _floofDb.SaveChangesAsync();
                
                await Context.Channel.SendMessageAsync($"{role.Name} ({role.Id}) can now be joined by users!");
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured when trying to add that role.");
                
                Log.Error("Error when trying to add a joinable role: " + ex);
            }
        }

        [Summary("Configure a role to no longer be joinable")]
        [Command("remove")]
        public async Task RemoveRole([Summary("role name")][Remainder]string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync(string.Empty, false, new EmbedBuilder { Description = $"💾 Usage: `iamconfig remove [rolename]`", Color = EMBED_COLOUR }.Build());
                return;
            }

            // Find the role by iterating over guild roles
            var role = GetRole(roleName.ToLower(), Context.Guild);
            
            if (role == null)
            {
                await Context.Channel.SendMessageAsync("Unable to find a role with that name to remove.");
                return;
            }

            var roleExistsInDb = CheckRoleEntryExists(role, Context.Guild);
            if (!roleExistsInDb)
            {
                await Context.Channel.SendMessageAsync("Users already cannot join that role.");
                return;
            }

            try
            {
                var roleDbEntry = _floofDb.UserAssignableRoles.AsQueryable().Where(r => r.ServerId == Context.Guild.Id).First(r => r.RoleId == role.Id);
                
                _floofDb.Remove(roleDbEntry);
                
                await _floofDb.SaveChangesAsync();
                
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