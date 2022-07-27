using Discord.WebSocket;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Floofbot.Services
{
    class WelcomeGateService
    {
        public async Task HandleWelcomeGate(SocketGuildUser before, SocketGuildUser after)
        {
            if (before.IsPending == after.IsPending) // no welcome gate change
                return;

            // We don't need to use a global variable for our context.
            // We use a using statement for that to empty resources when we are done with what we want to do
            await using (var floofDb = new FloofDataContext())
            {
                var guild = after.Guild;
                var serverConfig = await floofDb.WelcomeGateConfigs.FindAsync(guild.Id);

                if (serverConfig == null || serverConfig.Toggle == false || serverConfig.RoleId == null) // disabled
                    return;

                try
                {
                    var userRole = guild.GetRole((ulong) serverConfig.RoleId);
                    
                    if (userRole == null)// role does not exist anymore
                    {
                        Log.Error("Unable to automatically assign a role for the welcome gate - role does not exist");
                        return; 
                    }

                    await after.AddRoleAsync(userRole);
                }
                catch (Exception ex)
                {
                    Log.Error("An exception occured when trying to add roles for the welcome gate: " + ex);
                }
            }
        }
    }
}
