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
        public async Task HandleWelcomeGate(SocketGuildUser user)
        {
            FloofDataContext floofDb = new FloofDataContext();
            var guild = user.Guild;
            WelcomeGate serverConfig = floofDb.WelcomeGateConfigs.Find(guild.Id);

            if (serverConfig == null || serverConfig.Toggle == false || serverConfig.RoleId == null) // disabled
                return;

            try
            {
                var userRole = guild.GetRole((ulong)serverConfig.RoleId);
                if (userRole == null)
                    return; // role does not exist anymore

                await user.AddRoleAsync(userRole);
            }
            catch (Exception ex)
            {
                Log.Error("An exception occured when trying to add roles for the welcome gate: " + ex.ToString());
            }
        }
    }
}
