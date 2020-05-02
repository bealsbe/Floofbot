using Discord;
using Floofbot.Services.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Floofbot.Services
{
    public class NicknameAlert
    {
        private FloofDataContext _floofDb;
        private IGuildUser _user;
        public NicknameAlert(FloofDataContext floofDb)
        {
            _floofDb = floofDb;
            //_user = user;
        }

    }
}
