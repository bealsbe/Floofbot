using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

namespace Floofbot
{
    [Summary("Conversion commands")]
    [Discord.Commands.Name("Conversions")]
    [Group("convert")]
    public class Conversions : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("temp")]
        [Summary("Converts a temperature. Arguments are `[unit]` and `[temperature]`.")]
        public async Task Temp(string unit, double? input = null)
        {
            if(unit == "F" || unit == "f")
            {
                if(input != null)
                {
                    double? Cel;
                    Cel = (input - 32) / 1.8;

                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Title = "Temperature conversion",
                        Description=$"🌡 {(double)input}°F is equal to {(double)Cel}°C.",
                        //Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                        Color = EMBED_COLOR
                    };

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Please enter a temperature to convert.");
                }
            }
            else if(unit == "C" || unit == "c")
            {
                if(input != null)
                {
                    double? Fah;
                    Fah = (input * 1.8) + 32;

                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Title = "Temperature conversion",
                        Description = $"🌡 {(double)input}°C is equal to {(double)Fah}°F.",
                        Color = EMBED_COLOR
                    };

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Please enter a temperature to convert.");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Please enter a valid base unit for the first argument. Possible values are `[C]` or `[F]`.");
            }
        }
        [Command("weight")]
        [Summary("Converts a weight. Arguments are `[unit]` and `[weight]`.")]
        public async Task Weight(string unit, double? input = null)
        {
            double? kg = null;
            double? lb = null;
            if (unit == "Kg" || unit == "kg")
            {
                if (input != null)
                {
                    lb = (input * 2.2046);

                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Title = "Weight conversion",
                        Description = $"⚖️ {(double)input}Kg is equal to {(double)lb}lbs.",
                        Color = EMBED_COLOR
                    };

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Please enter a weight to convert.");
                }
            }
            else if (unit == "lb")
            {
                if (input != null)
                {
                    kg = (input * 0.4536);

                    EmbedBuilder builder = new EmbedBuilder()
                    {
                        Title = "Weight conversion",
                        Description = $"⚖️ {(double)input}lbs is equal to {(double)kg}Kg.",
                        Color = EMBED_COLOR
                    };

                    await Context.Channel.SendMessageAsync("", false, builder.Build());
                }
                else
                {
                    await Context.Channel.SendMessageAsync("Please enter a weight to convert.");
                }
            }
            else
            {
                await Context.Channel.SendMessageAsync("Please enter a valid base unit for the first argument. Possible values are `[Kg]` or `[lb]`.");
            }
        }
    }
}