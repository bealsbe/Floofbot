using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    class UserRoleRetentionService
    {
        FloofDataContext _floofDb;
        public UserRoleRetentionService(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }
        private SocketRole GetRole(string roleId, IGuild guild)
        {
            foreach (SocketRole r in guild.Roles)
            {
                if (r.Id.ToString() == roleId)
                    return r;
            }
            return null;
        }
        public async Task LogUserRoles(IGuildUser user)
        {
            Console.WriteLine("ping!");
            string userRoles = string.Join(",", user.RoleIds);
            Console.WriteLine(userRoles);
            await RemoveUserRoleLog(user);

            _floofDb.UserRolesLists.Add(new UserRolesList{
                ListOfRoleIds = userRoles,
                ServerId = user.Guild.Id,
                UserID = user.Id
            });
            await _floofDb.SaveChangesAsync();
        }
        public async Task RemoveUserRoleLog(IGuildUser user)
        {
            // clear old roles in db with new ones if they exist
            if (_floofDb.UserRolesLists.AsQueryable().Where(x => x.ServerId == user.Guild.Id && x.UserID == user.Id).Any())
            {
                var oldUserRoles = _floofDb.UserRolesLists.AsQueryable().Where(x => x.ServerId == user.Guild.Id && x.UserID == user.Id).FirstOrDefault();
                _floofDb.UserRolesLists.Remove(oldUserRoles);
                await _floofDb.SaveChangesAsync();
            }
        }
        public string GetUserRoles(IGuildUser user)
        {
            if (_floofDb.UserRolesLists.AsQueryable().Where(x => x.ServerId == user.Guild.Id && x.UserID == user.Id).Any())
            {
                var userRoles = _floofDb.UserRolesLists.AsQueryable().Where(x => x.ServerId == user.Guild.Id && x.UserID == user.Id).FirstOrDefault();
                return userRoles.ListOfRoleIds;
            }
            else
            {
                return null;
            }
        }
        public async Task RestoreUserRoles(IGuildUser user)
        {
            var oldUserRoles = GetUserRoles(user);

            if (oldUserRoles != null) // user actually had old roles
            {
                var oldUserRolesList = oldUserRoles.Split(",");

                // restore each role 1 by 1
                foreach (string roleId in oldUserRolesList)
                {
                    SocketRole role = GetRole(roleId, user.Guild);

                    if (role == null) // role does not exist
                        continue;
                    else
                    {
                        try
                        {
                            await user.AddRoleAsync(role);
                        }
                        catch
                        {
                            continue; // try add next role
                        }
                    }
                }
            }

        }
    }
}
