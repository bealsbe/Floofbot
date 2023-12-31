using Discord;
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
        private FloofDataContext _floofDb;
        public WelcomeGateService(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
        }
        public async Task HandleWelcomeGate(Cacheable<SocketGuildUser, ulong> _before, SocketGuildUser after)
        {
            SocketGuildUser before = _before.Value;
            if (before.IsPending == after.IsPending) // no welcome gate change
                return;
            FloofDataContext floofDb = new FloofDataContext();
            var guild = after.Guild;
            WelcomeGate serverConfig = floofDb.WelcomeGateConfigs.Find(guild.Id);

            if (serverConfig == null || serverConfig.Toggle == false || serverConfig.RoleId == null) // disabled
                return;

            try
            {
                var userRole = guild.GetRole((ulong)serverConfig.RoleId);
                if (userRole == null)// role does not exist anymore
                {
                    Log.Error("Unable to automatically assign a role for the welcome gate - role does not exist");
                    return; 
                }

                await after.AddRoleAsync(userRole);
            }
            catch (Exception ex)
            {
                Log.Error("An exception occured when trying to add roles for the welcome gate: " + ex.ToString());
            }
        }
    }
}
