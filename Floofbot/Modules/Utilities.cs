using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Floofbot.Configs;
using System.Text.RegularExpressions;
using UnitsNet;

namespace Floofbot
{
    [Summary("Utility commands")]
    [Name("Utilities")]
    public class Utilities : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("ping")]
        [Summary("Responds with the ping in milliseconds")]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync(":owl:").ConfigureAwait(false);
            sw.Stop();
            await msg.DeleteAsync();

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Butts!",
                Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("userinfo")]
        [Summary("Displays information on a mentioned user. If no parameters are given, displays the user's own information")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task UserInfo(IGuildUser usr = null)
        {
            var user = usr ?? Context.User as IGuildUser;

            if (user == null)
                return;

            string avatar = "https://cdn.discordapp.com/attachments/440635657925165060/442039889475665930/Turqouise.jpg";

            // Get user's Discord joining date and time, in UTC
            string discordJoin = user.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");
            // Get user's Guild joining date and time, in UTC
            string guildJoin = user.JoinedAt?.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            if (user.AvatarId != null)
                avatar = user.GetAvatarUrl(ImageFormat.Auto, 512);

            string infostring = $"👥 **User info for {user.Mention}** \n";
            infostring +=
                 $"**User** : {user.Nickname ?? user.Username} ({user.Username}#{user.Discriminator})\n" +
                 $"**ID** : {user.Id}\n" +
                 $"**Discord Join Date** : {discordJoin} \n" +
                 $"**Guild Join Date** : {guildJoin}\n" +
                 $"**Status** : {user.Status}\n";

            EmbedBuilder builder = new EmbedBuilder
            {
                ThumbnailUrl = avatar,
                Description = infostring,
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("avatar")]
        [Summary("Displays a mentioned user's avatar. If no parameters are given, displays the user's own avatar")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Avatar([Remainder] IGuildUser user = null)
        {
            if (user == null)
                user = (IGuildUser)Context.User;

            var avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 512);
            EmbedBuilder builder = new EmbedBuilder()
            {
                Description = $"🖼️ **Avatar for:** {user.Mention}\n",
                ImageUrl = avatarUrl,
                Color = EMBED_COLOR

            };
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }

        [Command("embed")]
        [Summary("Repeats a message in an embed format")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task RepeatMessage([Remainder] string message =null)
        {
            if (message != null)
            {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Description = message,
                    Color = EMBED_COLOR
                };
                await Context.Channel.SendMessageAsync("", false, builder.Build());
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            else
            {
                await Context.Channel.SendMessageAsync("Usage: `.embed [message]`");
            }
        }

        [Command("echo")]
        [Summary("Repeats a text message directly")]
        [RequireContext(ContextType.Guild)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        public async Task EchoMessage([Remainder] string message = null)
        {
            if (message != null)
            {
                await Context.Channel.SendMessageAsync(message);
                await Context.Channel.DeleteMessageAsync(Context.Message.Id);
            }
            else
            {
                await Context.Channel.SendMessageAsync("Usage: `.echo [message]`");
            }
        }

        [Command("serverinfo")]
        [Summary("Returns information about the current server")]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task ServerInfo()
        {
            SocketGuild guild = Context.Guild;

            // Get Guild creation date and time, in UTC
            string guildCreated = guild.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            int numberTextChannels = guild.TextChannels.Count;
            int numberVoiceChannels = guild.VoiceChannels.Count;
            int daysOld = Context.Message.CreatedAt.Subtract(guild.CreatedAt).Days;
            string daysAgo = $" That's " + ((daysOld == 0) ? "today!" : (daysOld == 1) ? $"yesterday!" : $"{daysOld} days ago!");
            string createdAt = $"Created {guildCreated}." + daysAgo;
            int totalMembers = guild.MemberCount;
            int onlineUsers = guild.Users.Where(mem => mem.Status == UserStatus.Online).Count();
            int numberRoles = guild.Roles.Count;
            int numberEmojis = guild.Emotes.Count;
            uint colour = (uint)new Random().Next(0x1000000); // random hex

            EmbedBuilder embed = new EmbedBuilder();

            embed.WithDescription(createdAt)
                 .WithColor(new Discord.Color(colour))
                 .AddField("Users (Online/Total)", $"{onlineUsers}/{totalMembers}", true)
                 .AddField("Text Channels", numberTextChannels, true)
                 .AddField("Voice Channels", numberVoiceChannels, true)
                 .AddField("Roles", numberRoles, true)
                 .AddField("Emojis", numberEmojis, true)
                 .AddField("Owner", $"{guild.Owner.Username}#{guild.Owner.Discriminator}", true)
                 .WithFooter($"Server ID: {guild.Id}")
                 .WithAuthor(guild.Name)
                 .WithCurrentTimestamp();

            if (Uri.IsWellFormedUriString(guild.IconUrl, UriKind.Absolute))
                embed.WithThumbnailUrl(guild.IconUrl);

            await Context.Channel.SendMessageAsync("", false, embed.Build());

        }

        [RequireOwner]
        [Command("reloadconfig")]
        [Summary("Reloads the config file")]
        public async Task ReloadConfig()
        {
            try
            {
                BotConfigFactory.Reinitialize();
            }
            catch (InvalidDataException e)
            {
                await Context.Channel.SendMessageAsync(e.Message);
                return;
            }

            if (BotConfigFactory.Config.Activity != null)
            {
                await Context.Client.SetActivityAsync(BotConfigFactory.Config.Activity);
            }
            await Context.Channel.SendMessageAsync("Config reloaded successfully");
        }

        [Command("serverlist")]
        [Summary("Returns a list of servers that the bot is in")]
        [RequireOwner]
        public async Task ServerList()
        {
            List<SocketGuild> guilds = new List<SocketGuild>(Context.Client.Guilds);
            List<PaginatedMessage.Page> pages = new List<PaginatedMessage.Page>();

            foreach (SocketGuild g in guilds)
            {
                List<EmbedFieldBuilder> fields = new List<EmbedFieldBuilder>();

                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Owner",
                    Value = $"{g.Owner.Username}#{g.Owner.Discriminator} | ``{g.Owner.Id}``",
                    IsInline = false
                });
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Server ID",
                    Value = g.Id,
                    IsInline = false
                });
                fields.Add(new EmbedFieldBuilder()
                {
                    Name = $"Members",
                    Value = g.MemberCount,
                    IsInline = false
                });

                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = g.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    ThumbnailUrl = (Uri.IsWellFormedUriString(g.IconUrl, UriKind.Absolute) ? g.IconUrl : null)
                });
            }
            var pager = new PaginatedMessage
            {
                Pages = (IEnumerable<object>)pages,
                Color = Color.DarkGreen,
                Content = "Here are a list of servers that I am in!",
                //FooterOverride = null,
                Options = PaginatedAppearanceOptions.Default,
                //TimeStamp = DateTimeOffset.UtcNow
            };
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            }, true);
        }

        [Command("convert")]
        [Alias("conv")]
        [Summary("Converts between Temperature, Length, and Mass units.")]
        public async Task convert([Remainder] string input = "")
        {
            Regex fahReg = new Regex(@"\d+(?=f)", RegexOptions.IgnoreCase);
            Regex celReg = new Regex(@"\d+(?=c)", RegexOptions.IgnoreCase);
            Regex miReg = new Regex(@"\d+(?=mi)", RegexOptions.IgnoreCase);
            Regex kmReg = new Regex(@"\d+(?=km)", RegexOptions.IgnoreCase);
            Regex kgReg = new Regex(@"\d+(?=kg)", RegexOptions.IgnoreCase);
            Regex lbReg = new Regex(@"\d+(?=lbs)", RegexOptions.IgnoreCase);
            string embedDesc = "";
            int okInt = 0; //This will be used to check for command success

            if (fahReg.IsMatch(input))
            {
                okInt++;

                double fahTmp = Convert.ToDouble(Regex.Match(input, @"-?\d+(?=f)").Value);

                Temperature fah = Temperature.FromDegreesFahrenheit(fahTmp);
                double cel = Math.Round(fah.DegreesCelsius, 2, MidpointRounding.ToEven);

                embedDesc += $"🌡 {fah} is equal to {cel}°C.\n";
            }

            if (celReg.IsMatch(input))
            {
                okInt++;

                double celTmp = Convert.ToDouble(Regex.Match(input, @"-?\d+(?=c)").Value);

                Temperature cel = Temperature.FromDegreesCelsius(celTmp);
                double fah = Math.Round(cel.DegreesFahrenheit, 2, MidpointRounding.ToEven);

                embedDesc += $"🌡 {cel} is equal to {fah}F.\n";
            }

            if (miReg.IsMatch(input))
            {
                okInt++;

                double miTmp = Convert.ToDouble(Regex.Match(input, @"\d+(?=mi)").Value);

                Length mi = Length.FromMiles(miTmp);
                double km = Math.Round(mi.Kilometers, 3, MidpointRounding.ToEven);

                embedDesc += $"📏 {mi} is equal to {km}Km.\n";
            }

            if (kmReg.IsMatch(input))
            {
                okInt++;

                double kmTmp = Convert.ToDouble(Regex.Match(input, @"\d+(?=km)").Value);

                Length km = Length.FromKilometers(kmTmp);
                double mi = Math.Round(km.Miles, 3, MidpointRounding.ToEven);

                embedDesc += $"📏 {km} is equal to {mi}mi.\n";
            }

            if (kgReg.IsMatch(input))
            {
                okInt++;

                double kgTmp = Convert.ToDouble(Regex.Match(input, @"\d+(?=kg)").Value);


                Mass kg = Mass.FromKilograms(kgTmp);
                double lb = Math.Round(kg.Pounds, 3, MidpointRounding.ToEven);

                embedDesc += $"⚖️ {kg} is equal to {lb}lbs.\n";
            }

            if (lbReg.IsMatch(input))
            {
                okInt++;

                double lbTmp = Convert.ToDouble(Regex.Match(input, @"\d+(?=lbs)").Value);

                Mass lb = Mass.FromPounds(lbTmp);
                double kg = Math.Round(lb.Kilograms, 3, MidpointRounding.ToEven);

                embedDesc += $"⚖️ {lb} is equal to {kg}Kg.\n";
            }

            if (okInt == 0)
            {
                embedDesc += $"No unit has been entered, or it was not recognized. Available units are mi<->km, °C<->F, and kg<->lbs.";
            }


            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Conversion",
                Description = embedDesc,
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
            
        [Command("about")]
        [Summary("Information about the bot")]
        public async Task About()
        {
            EmbedBuilder embed = new EmbedBuilder();

            uint colour = (uint)new Random().Next(0x1000000); // generate random color

            embed.WithDescription(
                    "This discord bot was created by bealsbe on github! (https://github.com/bealsbe/Floofbot)")
                .WithColor(new Discord.Color(colour));

            await Context.Channel.SendMessageAsync("", false, embed.Build());
        }
    }
}
