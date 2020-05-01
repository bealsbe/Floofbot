using Discord;
using Floofbot.Services.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Floofbot.Services
{
    public class NicknameAlertService
    {
        private FloofDataContext _floofDb;
        private IGuildUser _user;
        public NicknameAlertService(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
            //_user = user;
        }

    }
}
