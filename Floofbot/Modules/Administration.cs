using System;
using Discord;
using System.Threading.Tasks;
using Discord.Commands;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Text;
using Microsoft.Data.Sqlite;

namespace Floofbot.Modules
{
    public class Administration : ModuleBase<SocketCommandContext>
    {
        SqliteConnection dbConnection = new SqliteConnection(new SqliteConnectionStringBuilder {
            DataSource = "botdata.db"
        }.ToString());


        [Command("ban")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task YeetUser(string input, [Remainder] string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(input);
            if(badUser == null) {
               await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{input}\"");
               return;
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "⚖️ Ban Notification";
            builder.Description = $"You have been banned from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            //bans the user
            await Context.Guild.AddBanAsync(badUser.Id, 0, $"{Context.User.Username}#{Context.User.Discriminator} -> {reason}");

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Banned");
            builder.Color = Color.DarkOrange;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been banned from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("kick")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task kickUser(string user, [Remainder]string reason = "No Reason Provided")
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not resolve user: \"{user}\"");
                return;
            }

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "🥾 Kick Notification";
            builder.Description = $"You have been Kicked from {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            //kicks users
            await Context.Guild.GetUser(badUser.Id).KickAsync(reason);
            builder = new EmbedBuilder();
            builder.Title = ("🥾 User Kicked");
            builder.Color = Color.DarkOrange;
            builder.Description = $"{badUser.Username}#{badUser.Discriminator} has been kicked from {Context.Guild.Name}";
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("warn")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnUser(string user, string reason)
        {
            IUser badUser = resolveUser(user);
            if(badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            string sql = @"INSERT into Warnings(DateAdded,Forgiven,ForgivenBy, GuildId,Moderator,Reason,UserId)
                 VALUES($DateAdded,$Forgiven,$ForgivenBy,$GuildId,$Moderator,$Reason,$UserId)";
            SqliteCommand command = new SqliteCommand(sql, dbConnection);

            command.Parameters.Add(new SqliteParameter("$DateAdded", DateTime.Now.ToString()));
            command.Parameters.Add(new SqliteParameter("$Forgiven", "0")); // you are not forgiven for your sins
            command.Parameters.Add(new SqliteParameter("$ForgivenBy", ""));
            command.Parameters.Add(new SqliteParameter("$GuildId", Context.Guild.Id.ToString()));
            command.Parameters.Add(new SqliteParameter("$Moderator", $"{Context.User.Username}#{Context.User.Discriminator}"));
            command.Parameters.Add(new SqliteParameter("$Reason", reason));
            command.Parameters.Add(new SqliteParameter("$UserId", badUser.Id.ToString()));

            dbConnection.Open();
            command.ExecuteScalar();
            dbConnection.Close();

            //sends message to user
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "⚖️ Warn Notification";
            builder.Description = $"You have recieved a warning in {Context.Guild.Name}";
            builder.AddField("Reason", reason);
            builder.Color = Color.DarkOrange;
            await badUser.SendMessageAsync("", false, builder.Build());

            builder = new EmbedBuilder();
            builder.Title = (":shield: User Warned");
            builder.Color = Color.DarkOrange;
            builder.AddField("User ID", badUser.Id);
            builder.AddField("Moderator", $"{Context.User.Username}#{Context.User.Discriminator}");

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("warnlog")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.BanMembers)]
        public async Task warnlog(string user)
        {
            IUser badUser = resolveUser(user);
            if (badUser == null) {
                await Context.Channel.SendMessageAsync($"⚠️ Could not find user \"{user}\"");
                return;
            }

            string sql = @"SELECT DateAdded,Moderator,Reason FROM Warnings
                 WHERE UserID = $UserId AND $GuildId = $GuildId ORDER BY Id desc";
            SqliteCommand command = new SqliteCommand(sql, dbConnection);
            command.Parameters.Add(new SqliteParameter("$UserId", badUser.Id.ToString()));
            command.Parameters.Add(new SqliteParameter("$GuildId", Context.Guild.Id.ToString())); // you are not forgiven for your sins

            dbConnection.Open();
            var results = command.ExecuteReader();

            EmbedBuilder builder = new EmbedBuilder();
            if (results.HasRows) {
                builder.Title = $"Warnings for {badUser.Username}#{badUser.Discriminator}";
                builder.Color = Color.DarkOrange;

                //discord embeds have a limit of 25 fields.  Shows the most recent 25.
                int warningCount = 0;

                while (results.Read()) {
                    var warning = new {
                        DateAdded = results.GetValue(results.GetOrdinal("DateAdded")).ToString(),
                        Moderator = results.GetValue(results.GetOrdinal("Moderator")).ToString(),
                        Reason = results.GetValue(results.GetOrdinal("Reason")).ToString(),
                    };

                    builder.AddField($"**{warningCount + 1}**.  {DateTime.Parse(warning.DateAdded).ToString("MMMM dd yyyy")} by {warning.Moderator}", $"```{warning.Reason}```");
                    warningCount++;

                    //if we reach more then 25 warnings for a user then we are doing something wrong
                    if (warningCount > 24)
                        break;
                }
                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else {
                await Context.Channel.SendMessageAsync($"{badUser.Username}#{badUser.Discriminator} is a good noodle. They have no warnings!");
            }
            dbConnection.Close();
        }

        private IUser resolveUser(string input)
        {
            IUser user = null;
            //resolve userID or @mention
            if (Regex.IsMatch(input, @"\d{17,18}")) {
                string userID = Regex.Match(input, @"\d{17,18}").Value;
                user = Context.Client.GetUser(Convert.ToUInt64(userID));
            }
            //resolve username#0000
            else if (Regex.IsMatch(input, ".*#[0-9]{4}")) {
                string[] splilt = input.Split("#");
                user = Context.Client.GetUser(splilt[0], splilt[1]);
            }
            return user;
        }
    }
}
