using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Configs;
using Floofbot.Services.Repository;
using Floofbot.Services.Repository.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Send a message directly to the server's moderators")]
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
        public async Task sendModMail([Summary("Message Content")][Remainder] string content = "")
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                {
                    EmbedBuilder b;
                    b = new EmbedBuilder()
                    {
                        Description = $"Usage: `modmail [message]`",
                        Color = Color.Magenta
                    };
                    await Context.Message.Author.SendMessageAsync("", false, b.Build());
                }

                // get values
                ModMail serverConfig = _floofDb.ModMails.Find(SERVER_ID);
                IGuild guild = Context.Client.GetGuild(SERVER_ID); // can return null
                Discord.ITextChannel channel = await guild.GetTextChannelAsync((ulong)serverConfig.ChannelId); // can return null
                IRole role = null;

                if (Context.User.MutualGuilds.Count() == 0) // no mutual guilds, disable modmail
                    return;
                if (serverConfig == null || serverConfig.IsEnabled == false || guild == null || channel == null) // not configured
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

                // form embed
                SocketUser sender = Context.Message.Author;
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "⚠️ | MOD MAIL ALERT!",
                    Description = $"Modmail from: {sender.Mention} ({sender.Username}#{sender.Discriminator})",
                    Color = Discord.Color.Gold
                };
                builder.WithCurrentTimestamp();
                builder.AddField("Message Content", $"```{content}```");
                string messageContent = (role == null) ? "Mod mail" : role.Mention; // role id can be set in database but deleted from server
                await Context.Channel.SendMessageAsync("Alerting all mods!");
                await channel.SendMessageAsync(messageContent, false, builder.Build());
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync(ex.ToString());
                return;
            }
        }
    }
    [Summary("Modmail configuration commands")]
    [RequireUserPermission(GuildPermission.Administrator)]
    [Group("modmailconfig")]
    public class ModMailConfigModule : InteractiveBase
    {
        private FloofDataContext _floofDB;

        public ModMailConfigModule(FloofDataContext floofDB)
        {
            _floofDB = floofDB;
        }
        private Discord.Color GenerateColor()
        {
            return new Discord.Color((uint)new Random().Next(0x1000000));
        }

        private ModMail GetServerConfig(ulong server)
        {
            // checks if server exists in database and adds if not
            var serverConfig = _floofDB.ModMails.Find(server);
            if (serverConfig == null)
            {
                _floofDB.Add(new ModMail
                {
                    ServerId = server,
                    ChannelId = null,
                    IsEnabled = false,
                    ModRoleId = null
                });
                _floofDB.SaveChanges();
                return _floofDB.ModMails.Find(server);
            }
            else
            {
                return serverConfig;
            }
        }

        [Command("channel")]
        [Summary("Sets the channel for the modmail notifications")]
        public async Task Channel([Summary("Channel (eg #alerts)")]Discord.IChannel channel = null)
        {
            if (channel == null)
            {
                channel = (IChannel)Context.Channel;
            }
            var ServerConfig = GetServerConfig(Context.Guild.Id);
            ServerConfig.ChannelId = channel.Id;
            _floofDB.SaveChanges();
            await Context.Channel.SendMessageAsync("Channel updated! I will send modmails to <#" + channel.Id + ">");
        }

        [Command("toggle")]
        [Summary("Toggles the modmail module")]
        public async Task Toggle()
        {

            // try toggling
            try
            {
                // check the status of logger
                var ServerConfig = GetServerConfig(Context.Guild.Id);
                if (ServerConfig.ChannelId == null)
                {
                    await Context.Channel.SendMessageAsync("Channel not set! Please set the channel before toggling the ModMail feature.");
                    return;
                }
                ServerConfig.IsEnabled = !ServerConfig.IsEnabled;
                _floofDB.SaveChanges();
                await Context.Channel.SendMessageAsync("Modmail " + (ServerConfig.IsEnabled ? "Enabled!" : "Disabled!"));
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to toggle the modmail: " + ex);
                return;
            }
        }
        [Command("modrole")]
        [Summary("OPTIONAL: A Role to Ping When ModMail is Received.")]
        public async Task SetModRole(string roleName = null)
        {
            if (roleName == null)
            {
                await Context.Channel.SendMessageAsync("", false, new EmbedBuilder { Description = $"💾 Usage: `modmailconfig [rolename]`", Color = GenerateColor() }.Build());
                return;
            }

            var ServerConfig = GetServerConfig(Context.Guild.Id);
            bool modRoleFound = false;
            ulong? roleId = null;
            try
            {
                foreach (SocketRole r in Context.Guild.Roles)
                {
                    if (r.Name.ToLower() == roleName.ToLower())
                    {
                        if (modRoleFound == false) // ok we found 1 role thats GOOD
                        {
                            modRoleFound = true;
                            roleId = r.Id;
                        }
                        else // there is more than 1 role with the same name!
                        {
                            await Context.Channel.SendMessageAsync("More than one role exists with that name! Not sure what to do! Please resolve this. Aborting..");
                            return;
                        }
                    }
                }

                if (modRoleFound)
                {
                    ServerConfig.ModRoleId = roleId;
                    _floofDB.SaveChanges();
                    await Context.Channel.SendMessageAsync("Mod role set!");
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Unable to find that role. Role not set.");
                    return;
                }
            }
            catch (Exception ex)
            {
                await Context.Channel.SendMessageAsync("An error occured: " + ex.Message);
                Log.Error("Error when trying to set the modmail mod role: " + ex);
            }
        }
    }

}
