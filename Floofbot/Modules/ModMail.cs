using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Send a message directly to the server's moderators")]
    [Name("ModMail")]
    [Group("modmail")]
    public class ModMailModule : InteractiveBase
    {
        private FloofDataContext _floofDb;
        static readonly ulong SERVER_ID = BotConfigFactory.Config.ModMailServer; // TODO: Replace this so that it works on more servers
        public ModMailModule(FloofDataContext _floofDB)
        {
            _floofDb = _floofDB;
        }

        [Command("")]
        [Name("modmail <content>")]
        [Summary("Send a message to the moderators")]
        public async Task SendModMail([Summary("Message Content")][Remainder] string content = "")
        {
            var serverConfig = await _floofDb.ModMails.FindAsync(SERVER_ID);
            
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    var b = new EmbedBuilder()
                    {
                        Description = $"Usage: `modmail [message]`",
                        Color = Color.Magenta
                    };
                    
                    await Context.Message.Author.SendMessageAsync(string.Empty, false, b.Build());
                }

                // Get values
                IGuild guild = Context.Client.GetGuild(SERVER_ID); // Can return null
                var channel = await guild.GetTextChannelAsync((ulong)serverConfig.ChannelId); // Can return null
                IRole role = null;

                if (!Context.User.MutualGuilds.Contains(guild)) // the modmail server is not a mutual server
                    return;

                if (serverConfig == null || serverConfig.IsEnabled == false || guild == null || channel == null) // Not configured
                {
                    await Context.Channel.SendMessageAsync("Modmail is not configured on this server.");
                    
                    return;
                }

                if (serverConfig.ModRoleId != null)
                {
                    role = guild.GetRole((ulong)serverConfig.ModRoleId); // can return null
                }

                if (content.Length > 500)
                {
                    await Context.Message.Author.SendMessageAsync("Mod mail content cannot exceed 500 characters");
                    return;
                }

                // Form embed
                var sender = Context.Message.Author;
                
                var builder = new EmbedBuilder()
                {
                    Title = "⚠️ | MOD MAIL ALERT!",
                    Description = $"Modmail from: {sender.Mention} ({sender.Username}#{sender.Discriminator})",
                    Color = Discord.Color.Gold
                }.WithCurrentTimestamp()
                 .AddField("Message Content", $"```{content}```");
                
                var messageContent = (role == null) ? "Mod mail" : role.Mention; // role id can be set in database but deleted from server
                
                await Context.Channel.SendMessageAsync("Alerting all mods!");
                
                await channel.SendMessageAsync(messageContent, false, builder.Build());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
            }
        }
    }
}
