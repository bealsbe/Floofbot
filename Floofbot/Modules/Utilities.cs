using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Floofbot
{
    public class Utilities : ModuleBase<SocketCommandContext>
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
        public async Task UserInfo(IGuildUser usr = null)
        {
            var user = usr ?? Context.User as IGuildUser;

            if (user == null)
                return;

            string avatar = "https://cdn.discordapp.com/attachments/440635657925165060/442039889475665930/Turqouise.jpg";

            if (user.AvatarId != null)
                avatar = user.GetAvatarUrl(ImageFormat.Auto, 512);

            string infostring = $"👥 **User info for:** {user.Mention}\n";
            infostring +=
                 $"**Username** : {user.Username}#{user.Discriminator}\n" +
                 $"**Nickname** : {user.Nickname ?? user.Username}\n" +
                 $"**ID** : {user.Id}\n" +
                 $"**Discord Join Date** : {user.CreatedAt:MM/dd/yyyy} \n" +
                 $"**Guild Join Date** : {user.JoinedAt?.ToString("MM/dd/yyyy")}\n" +
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

        [Command("say")]
        [Summary("Repeats a message")]
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
            }
            else
            {
                await Context.Channel.SendMessageAsync("Usage: `.say [message]`");
            }
        }

        [Command("serverinfo")]
        [Summary("Returns information about the current server")]
        public async Task ServerInfo()
        {
            SocketGuild guild = Context.Guild;
            int numberTextChannels = guild.TextChannels.Count;
            int numberVoiceChannels = guild.VoiceChannels.Count;
            string createdAt = $"Created {guild.CreatedAt.DateTime.ToShortDateString()}. " +
                               $"That's over {Context.Message.CreatedAt.Subtract(guild.CreatedAt).Days} days ago!";
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
    }
}
