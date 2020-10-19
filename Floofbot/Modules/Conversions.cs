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
        public async Task TempFC(string unit, double? input = null)
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
            else
            {
                await Context.Channel.SendMessageAsync("Please enter a valid base unit for the first argument. Possible values are `[C]` or `[F]`.");
            }
        }
    }
}