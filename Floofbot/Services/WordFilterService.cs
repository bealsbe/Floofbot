using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Floofbot.Services
{
    class WordFilterService
    {
        List<FilteredWord> _filteredWords;
        DateTime _lastRefreshedTime;

        public List<string> filteredWordsInName(FloofDataContext floofDb, string messageContent, ulong serverId) // names
        {
            // return false if none of the serverIds match or filtering has been disabled for the server
            if (!floofDb.FilterConfigs.AsQueryable()
                .Any(x => x.ServerId == serverId && x.IsOn))
            {
                return null;
            }

            DateTime currentTime = DateTime.Now;
            if (_lastRefreshedTime == null || currentTime.Subtract(_lastRefreshedTime).TotalMinutes >= 30)
            {
                _filteredWords = floofDb.FilteredWords.AsQueryable()
                    .Where(x => x.ServerId == serverId).ToList();
                _lastRefreshedTime = currentTime;
            }

            List<string> DetectedWords = new List<string>();

            foreach (var filteredWord in _filteredWords)
            {
                Regex r = new Regex($"{filteredWord.Word}",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (r.IsMatch(messageContent))
                {
                    DetectedWords.Add(filteredWord.Word);
                }
            }
            if (DetectedWords.Count() == 0)
                return null;
            else
                return DetectedWords;
        }
        public bool hasFilteredWord(FloofDataContext floofDb, string messageContent, ulong serverId, ulong channelId) // messages
        {
            // return false if none of the serverIds match or filtering has been disabled for the server
            if (!floofDb.FilterConfigs.AsQueryable()
                .Any(x => x.ServerId == serverId && x.IsOn))
            {
                return false;
            }

            // whitelist means we don't have the filter on for this channel
            if (floofDb.FilterChannelWhitelists.AsQueryable()
                .Any(x => x.ChannelId == channelId && x.ServerId == serverId))
            {
                return false;
            }

            DateTime currentTime = DateTime.Now;
            if (_lastRefreshedTime == null || currentTime.Subtract(_lastRefreshedTime).TotalMinutes >= 30)
            {
                _filteredWords = floofDb.FilteredWords.AsQueryable()
                    .Where(x => x.ServerId == serverId).ToList();
                _lastRefreshedTime = currentTime;
            }

            foreach (var filteredWord in _filteredWords)
            {
                Regex r = new Regex(@$"\b({filteredWord.Word})\b",
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
                if (r.IsMatch(messageContent))
                {
                    return true;
                }
            }
            return false;
        }
    }

}
