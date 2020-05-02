using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Floofbot
{
    public class Utilities : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
        [Summary("Responds with the ping in milliseconds")]
        public async Task Ping()
        {
            var sw = Stopwatch.StartNew();
            var msg = await Context.Channel.SendMessageAsync(":owl:").ConfigureAwait(false);
            sw.Stop();
            await msg.DeleteAsync();

            EmbedBuilder builder = new EmbedBuilder() {
                Title = "Butts!",
                Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = Color.Magenta
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

            EmbedBuilder builder = new EmbedBuilder {
                ThumbnailUrl = avatar,
                Description = infostring,
                Color = Color.Magenta
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
            EmbedBuilder builder = new EmbedBuilder() {
                Description = $"🖼️ **Avatar for:** {user.Mention}\n",
                ImageUrl = avatarUrl,
                Color = Color.Magenta

            };
            await Context.Channel.SendMessageAsync("", false, builder.Build());

        }
    }
}
