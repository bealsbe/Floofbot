using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace Floofbot.Modules
{
    public class Utilities : ModuleBase<SocketCommandContext>
    {
        [Command("ping")]
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
    }
}
