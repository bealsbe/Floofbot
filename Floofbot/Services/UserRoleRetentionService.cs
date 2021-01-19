using Discord;
using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Microsoft.EntityFrameworkCore.Internal;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
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
        public async Task LogUserRoles(IGuildUser user)
        {
            string userRoles = string.Join(",", user.RoleIds);
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
                    IRole role = user.Guild.GetRole(Convert.ToUInt64(roleId));

                    if (role == null) // role does not exist
                        continue;
                    else
                    {
                        try
                        {
                            await user.AddRoleAsync(role);
                        }
                        catch (Exception ex)
                        {
                            Log.Information("Cannot return the role ID " + role.Id + " to user ID " + user.Id + ". Error: " + ex.ToString());
                            continue; // try add next role
                        }
                    }
                }
            }

        }
    }
}
