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
        private static readonly Color EMBED_COLOR = Color.Magenta;

        [Command("ping")]
        [Summary("Responds with the ping in milliseconds")]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            
            var msg = await Context.Channel.SendMessageAsync(":owl:").ConfigureAwait(false);
            
            sw.Stop();
            
            await msg.DeleteAsync();

            var builder = new EmbedBuilder()
            {
                Title = "Butts!",
                Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
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

            var avatar = "https://cdn.discordapp.com/attachments/440635657925165060/442039889475665930/Turqouise.jpg";
            // Get user's Discord joining date and time, in UTC
            var discordJoin = user.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");
            // Get user's Guild joining date and time, in UTC
            var guildJoin = user.JoinedAt?.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            if (user.AvatarId != null)
                avatar = user.GetAvatarUrl(ImageFormat.Auto, 512);

            var infostring = $"👥 **User info for {user.Mention}** \n";
            
            infostring +=
                 $"**User** : {user.Nickname ?? user.Username} ({user.Username}#{user.Discriminator})\n" +
                 $"**ID** : {user.Id}\n" +
                 $"**Discord Join Date** : {discordJoin} \n" +
                 $"**Guild Join Date** : {guildJoin}\n" +
                 $"**Status** : {user.Status}\n";

            var builder = new EmbedBuilder
            {
                ThumbnailUrl = avatar,
                Description = infostring,
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
        }

        [Command("avatar")]
        [Summary("Displays a mentioned user's avatar. If no parameters are given, displays the user's own avatar")]
        [RequireContext(ContextType.Guild)]
        [RequireBotPermission(ChannelPermission.EmbedLinks)]
        public async Task Avatar([Remainder] IGuildUser user = null)
        {
            // If user is null, we assign Context.User to it
            user ??= (IGuildUser) Context.User;

            var avatarUrl = user.GetAvatarUrl(ImageFormat.Auto, 512);
            
            var builder = new EmbedBuilder()
            {
                Description = $"🖼️ **Avatar for:** {user.Mention}\n",
                ImageUrl = avatarUrl,
                Color = EMBED_COLOR
            };
            
            await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
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
                
                await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
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
            var guild = Context.Guild;

            // Get Guild creation date and time, in UTC
            var guildCreated = guild.CreatedAt.ToUniversalTime().ToString("dd\\/MMM\\/yyyy \\a\\t H:MM \\U\\T\\C");

            var numberTextChannels = guild.TextChannels.Count;
            var numberVoiceChannels = guild.VoiceChannels.Count;
            var daysOld = Context.Message.CreatedAt.Subtract(guild.CreatedAt).Days;
            var daysAgo = $" That's " + ((daysOld == 0) ? "today!" : (daysOld == 1) ? $"yesterday!" : $"{daysOld} days ago!");
            var createdAt = $"Created {guildCreated}." + daysAgo;
            var totalMembers = guild.MemberCount;
            var onlineUsers = guild.Users.Count(mem => mem.Status == UserStatus.Online);
            var numberRoles = guild.Roles.Count;
            var numberEmojis = guild.Emotes.Count;
            var colour = (uint)new Random().Next(0x1000000); // random hex

            var embed = new EmbedBuilder()
                .WithDescription(createdAt)
                .WithColor(new Color(colour))
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

            await Context.Channel.SendMessageAsync(string.Empty, false, embed.Build());
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
            catch (InvalidDataException ex)
            {
                await Context.Channel.SendMessageAsync(ex.Message);
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
            var guilds = new List<SocketGuild>(Context.Client.Guilds);
            var pages = new List<PaginatedMessage.Page>();

            foreach (var g in guilds)
            {
                var fields = new List<EmbedFieldBuilder>
                {
                    new EmbedFieldBuilder()
                    {
                        Name = $"Owner",
                        Value = $"{g.Owner.Username}#{g.Owner.Discriminator} | ``{g.Owner.Id}``",
                        IsInline = false
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = $"Server ID",
                        Value = g.Id,
                        IsInline = false
                    },
                    new EmbedFieldBuilder()
                    {
                        Name = $"Members",
                        Value = g.MemberCount,
                        IsInline = false
                    }
                };

                pages.Add(new PaginatedMessage.Page
                {
                    Author = new EmbedAuthorBuilder { Name = g.Name },
                    Fields = new List<EmbedFieldBuilder>(fields),
                    ThumbnailUrl = (Uri.IsWellFormedUriString(g.IconUrl, UriKind.Absolute) ? g.IconUrl : null)
                });
            }
            
            var pager = new PaginatedMessage
            {
                Pages = pages,
                Color = Color.DarkGreen,
                Content = "Here are a list of servers that I am in!",
                FooterOverride = null,
                Options = PaginatedAppearanceOptions.Default,
                TimeStamp = DateTimeOffset.UtcNow
            };
            
            await PagedReplyAsync(pager, new ReactionList
            {
                Forward = true,
                Backward = true,
                Jump = true,
                Trash = true
            });
        }

        [Command("convert")]
        [Alias("conv")]
        [Summary("Converts between Temperature, Length, and Mass units.")]
        public async Task Convert([Remainder] string input = "")
        {
            var fahReg = new Regex(@"\d+(?=f)", RegexOptions.IgnoreCase);
            var celReg = new Regex(@"\d+(?=c)", RegexOptions.IgnoreCase);
            var miReg = new Regex(@"\d+(?=mi)", RegexOptions.IgnoreCase);
            var kmReg = new Regex(@"\d+(?=km)", RegexOptions.IgnoreCase);
            var kgReg = new Regex(@"\d+(?=kg)", RegexOptions.IgnoreCase);
            var lbReg = new Regex(@"\d+(?=lbs)", RegexOptions.IgnoreCase);
            var embedDesc = string.Empty;
            var okInt = 0; // This will be used to check for command success

            if (fahReg.IsMatch(input))
            {
                okInt++;

                var fahTmp = System.Convert.ToDouble(Regex.Match(input, @"-?\d+(?=f)").Value);

                var fah = Temperature.FromDegreesFahrenheit(fahTmp);
                var cel = Math.Round(fah.DegreesCelsius, 2, MidpointRounding.ToEven);

                embedDesc += $"🌡 {fah} is equal to {cel}°C.\n";
            }

            if (celReg.IsMatch(input))
            {
                okInt++;

                var celTmp = System.Convert.ToDouble(Regex.Match(input, @"-?\d+(?=c)").Value);

                var cel = Temperature.FromDegreesCelsius(celTmp);
                var fah = Math.Round(cel.DegreesFahrenheit, 2, MidpointRounding.ToEven);

                embedDesc += $"🌡 {cel} is equal to {fah}F.\n";
            }

            if (miReg.IsMatch(input))
            {
                okInt++;

                var miTmp = System.Convert.ToDouble(Regex.Match(input, @"\d+(?=mi)").Value);

                var mi = Length.FromMiles(miTmp);
                var km = Math.Round(mi.Kilometers, 3, MidpointRounding.ToEven);

                embedDesc += $"📏 {mi} is equal to {km}Km.\n";
            }

            if (kmReg.IsMatch(input))
            {
                okInt++;

                var kmTmp = System.Convert.ToDouble(Regex.Match(input, @"\d+(?=km)").Value);

                var km = Length.FromKilometers(kmTmp);
                var mi = Math.Round(km.Miles, 3, MidpointRounding.ToEven);

                embedDesc += $"📏 {km} is equal to {mi}mi.\n";
            }

            if (kgReg.IsMatch(input))
            {
                okInt++;

                var kgTmp = System.Convert.ToDouble(Regex.Match(input, @"\d+(?=kg)").Value);
                
                var kg = Mass.FromKilograms(kgTmp);
                var lb = Math.Round(kg.Pounds, 3, MidpointRounding.ToEven);

                embedDesc += $"⚖️ {kg} is equal to {lb}lbs.\n";
            }

            if (lbReg.IsMatch(input))
            {
                okInt++;

                var lbTmp = System.Convert.ToDouble(Regex.Match(input, @"\d+(?=lbs)").Value);

                var lb = Mass.FromPounds(lbTmp);
                var kg = Math.Round(lb.Kilograms, 3, MidpointRounding.ToEven);

                embedDesc += $"⚖️ {lb} is equal to {kg}Kg.\n";
            }

            if (okInt == 0)
            {
                embedDesc += $"No unit has been entered, or it was not recognized. Available units are mi<->km, °C<->F, and kg<->lbs.";
            }
            
            var builder = new EmbedBuilder()
            {
                Title = "Conversion",
                Description = embedDesc,
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync(string.Empty, false, builder.Build());
        }
            
        [Command("about")]
        [Summary("Information about the bot")]
        public async Task About()
        {
            var embed = new EmbedBuilder();

            var colour = (uint) new Random().Next(0x1000000); // Generate random color

            embed.WithDescription("This discord bot was created by bealsbe on github! (https://github.com/bealsbe/Floofbot)")
                .WithColor(new Color(colour));

            await Context.Channel.SendMessageAsync(string.Empty, false, embed.Build());
        }
    }
}
