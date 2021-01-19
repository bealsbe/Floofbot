using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Commands to blacklist users from the bot commands.")]
    [Name("Blacklist")]
    [Group("blacklist")]
    public class BlacklistUser : InteractiveBase
    {
        FloofDataContext _floofDb;
        public BlacklistUser(FloofDataContext floofDb) => _floofDb = floofDb;

        [Command("server")]
        [RequireUserPermission(Discord.GuildPermission.Administrator)]
        [RequireContext(ContextType.Guild)]
        [Summary("(Un)Ban a user from using the bot commands within a specific server")]
        public async Task ServerBlacklist([Summary("The ID of the user")] string userId)
        {
            var badUser = ResolveUser(userId);
            ulong badUserId;
            BlacklistedUser blacklistedUser;

            // get user from database
            if (badUser == null) // user not resolved
            {
                if (Regex.IsMatch(userId, @"\d{16,}")) // use user id instead
                {
                    blacklistedUser = GetBlacklistedUser(Convert.ToUInt64(userId), Context.Guild.Id, false);
                    badUserId = Convert.ToUInt64(userId);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("⚠️ Unable to find that user.");
                    return;
                }
            }
            else
            {
                blacklistedUser = GetBlacklistedUser(badUser.Id, Context.Guild.Id, false);
                badUserId = badUser.Id;
            }

            if (blacklistedUser == null) // user is not currently blacklisted from server
            {
                // does this user exist in the database at all?
                var userDbEntry = _floofDb.BlacklistedUsers.AsQueryable().Where(x => x.UserID == badUserId).FirstOrDefault();

                if (userDbEntry == null) // user isnt in database so add new entry
                {
                    await _floofDb.BlacklistedUsers.AddAsync(new BlacklistedUser
                    {
                        UserID = badUserId,
                        IsGlobal = false,
                        BannedFromServers = Context.Guild.Id.ToString()
                    });
                    await _floofDb.SaveChangesAsync();
                }
                else // user is in database but not blacklisted from server, append server id
                {
                    if (string.IsNullOrEmpty(userDbEntry.BannedFromServers)) // not banned from any servers just globally
                    {
                        userDbEntry.BannedFromServers = Context.Guild.Id.ToString();
                    }
                    else // banned from other servers, append this one
                    {
                        Console.WriteLine(userDbEntry.BannedFromServers);
                        var usersBlacklistedServers = userDbEntry.BannedFromServers.Split(",");
                        usersBlacklistedServers.Append(Context.Guild.Id.ToString());
                        foreach (string s in usersBlacklistedServers)
                            Console.WriteLine(s);

                        userDbEntry.BannedFromServers = string.Join(",", usersBlacklistedServers);
                        Console.WriteLine(userDbEntry.BannedFromServers);
                    }
                    await _floofDb.SaveChangesAsync();
                }
                await Context.Channel.SendMessageAsync("User ID " + badUserId + " will be ignored in this server!");
                return;
            }
            else // blacklisted user exists, lets remove their server blacklist
            {
                var usersBlacklistedServers = blacklistedUser.BannedFromServers.Split(",").Where(x => x != Context.Guild.Id.ToString()); // take all values that arent this guild id
                if (usersBlacklistedServers.Count() == 0 && blacklistedUser.IsGlobal == false) // user isnt blacklisted anywhere else so lets remove the db entry
                {
                    _floofDb.Remove(blacklistedUser);
                    await _floofDb.SaveChangesAsync();
                }
                else
                {
                    blacklistedUser.BannedFromServers = string.Join(",", usersBlacklistedServers);
                    await _floofDb.SaveChangesAsync();
                }
                await Context.Channel.SendMessageAsync("User ID " + badUserId + " will no longer be ignored in this server!");
                return;
            }
        }

        [Command("global")]
        //[RequireOwner]
        [Summary("(Un)Ban a user from using the bot commands globally")]
        public async Task GlobalBlacklist(string userId)
        {
            var badUser = ResolveUser(userId);
            ulong badUserId;
            BlacklistedUser blacklistedUser;

            // get user from database
            if (badUser == null) // user not resolved
            {
                if (Regex.IsMatch(userId, @"\d{16,}")) // use user id instead
                {
                    blacklistedUser = GetBlacklistedUser(Convert.ToUInt64(userId), 0, true);
                    badUserId = Convert.ToUInt64(userId);
                }
                else
                {
                    await Context.Channel.SendMessageAsync("⚠️ Unable to find that user.");
                    return;
                }
            }
            else
            {
                blacklistedUser = GetBlacklistedUser(badUser.Id, 0, true);
                badUserId = badUser.Id;
            }
            if (blacklistedUser == null) // user does not exist in db
            {

                await _floofDb.BlacklistedUsers.AddAsync(new BlacklistedUser
                {
                    UserID = badUserId,
                    IsGlobal = true,
                    BannedFromServers = ""
                });
                await _floofDb.SaveChangesAsync();
                await Context.Channel.SendMessageAsync("User ID " + badUserId + " is now being blacklisted globally!");
            }
            else // user exists in db
            {
                if (string.IsNullOrEmpty(blacklistedUser.BannedFromServers) && blacklistedUser.IsGlobal == true) // user isnt blacklisted anywhere anymore so just remove them from db
                {
                    _floofDb.Remove(blacklistedUser);
                    await Context.Channel.SendMessageAsync("User ID " + badUserId + " is no longer being blacklisted anywhere!");
                }
                else
                {
                    blacklistedUser.IsGlobal = !blacklistedUser.IsGlobal;
                    await Context.Channel.SendMessageAsync("User ID " + badUserId + " is now " + (blacklistedUser.IsGlobal ? "blacklisted globally!" : "no longer blacklisted globally!"));
                }
                await _floofDb.SaveChangesAsync();
            }

        }

        [Command("list")]
        [RequireOwner]
        [Summary("View the list of users blacklisted from the bot")]
        public async Task ViewBlacklist()
        {

        }
        public BlacklistedUser GetBlacklistedUser(ulong userId, ulong serverId, bool isGlobalSearch)
        {
            BlacklistedUser blacklistedUser = null;
            if (isGlobalSearch)
            {
                bool entryExists = _floofDb.BlacklistedUsers.AsQueryable().Where(x => x.UserID == userId && x.IsGlobal == true).Any();
                if (entryExists)
                    blacklistedUser = _floofDb.BlacklistedUsers.AsQueryable().Where(x => x.UserID == userId && x.IsGlobal == true).FirstOrDefault();
            }
            else
            {
                bool entryExists = _floofDb.BlacklistedUsers.AsQueryable().Where(x => x.UserID == userId && x.BannedFromServers.Contains(serverId.ToString())).Any();
                if (entryExists)
                    blacklistedUser = _floofDb.BlacklistedUsers.AsQueryable().Where(x => x.UserID == userId && x.BannedFromServers.Contains(serverId.ToString())).FirstOrDefault();
            }
            return blacklistedUser; // returns null if no database entry
        }

        private IUser ResolveUser(string input)
        {
            IUser user = null;
            //resolve userID or @mention
            if (Regex.IsMatch(input, @"\d{16,}"))
            {
                string userID = Regex.Match(input, @"\d{16,}").Value;
                user = Context.Client.GetUser(Convert.ToUInt64(userID));
            }
            //resolve username#0000
            else if (Regex.IsMatch(input, ".*#[0-9]{4}"))
            {
                string[] splilt = input.Split("#");
                user = Context.Client.GetUser(splilt[0], splilt[1]);
            }
            return user;
        }
    }
}
