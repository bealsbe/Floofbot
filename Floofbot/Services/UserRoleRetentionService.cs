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
using Microsoft.EntityFrameworkCore;

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
            var userRoles = string.Join(",", user.RoleIds);
            
            await RemoveUserRoleLog(user);

            _floofDb.UserRolesLists.Add(new UserRolesList{
                ListOfRoleIds = userRoles,
                ServerId = user.Guild.Id,
                UserID = user.Id,
                UTCTimestamp = DateTime.Now
            });
            
            await _floofDb.SaveChangesAsync();
        }

        private async Task RemoveUserRoleLog(IGuildUser user)
        {
            // clear old roles in db with new ones if they exist
            if (_floofDb.UserRolesLists.AsQueryable().Any(x => x.ServerId == user.Guild.Id && x.UserID == user.Id))
            {
                var oldUserRoles = await _floofDb.UserRolesLists.AsQueryable().FirstOrDefaultAsync(x => x.ServerId == user.Guild.Id && x.UserID == user.Id);
                
                _floofDb.UserRolesLists.Remove(oldUserRoles);
                
                await _floofDb.SaveChangesAsync();
            }
        }

        private string GetUserRoles(IGuildUser user)
        {
            if (_floofDb.UserRolesLists.AsQueryable().Any(x => x.ServerId == user.Guild.Id && x.UserID == user.Id))
            {
                var userRoles = _floofDb.UserRolesLists.AsQueryable().FirstOrDefault(x => x.ServerId == user.Guild.Id && x.UserID == user.Id);
               
                return userRoles.ListOfRoleIds;
            }

            return null;
        }
        
        public async Task RestoreUserRoles(IGuildUser user)
        {
            var oldUserRoles = GetUserRoles(user);

            if (oldUserRoles != null) // User actually had old roles
            {
                var oldUserRolesList = oldUserRoles.Split(",");

                // Restore each role 1 by 1
                foreach (var roleId in oldUserRolesList)
                {
                    var role = user.Guild.GetRole(Convert.ToUInt64(roleId));

                    if (role == null) // role does not exist
                    {
                        Log.Error("Unable to return role ID " + roleId + " to user ID " + user.Id + " as it does not exist anymore!");
                        continue;
                    }

                    if (role.Name == "@everyone")
                        continue;

                    try
                    {
                        await user.AddRoleAsync(role);
                    }
                    catch (Exception ex)
                    {
                        Log.Information("Cannot return the role ID " + role.Id + " to user ID " + user.Id + ". Error: " + ex);
                    }
                }
            }

        }
    }
}
