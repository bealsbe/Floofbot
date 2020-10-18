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
    public class Conversions : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("tempfc")]
        [Summary("Converts a temperature from Fahrenheit to Celsius")]
        public async Task TempFC(double Fah)
        {
            double Cel;
            Cel = (Fah - 32) / 1.8;

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Temperature conversion",
                Description=$"🌡 {(double)Fah}°F is equal to {(double)Cel}°C.",
                //Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
        [Command("tempcf")]
        [Summary("Converts a temperature from Celsius to Fahrenheit")]
        public async Task TempCF(double Cel)
        {
            double Fah;
            Fah = (Cel * 1.8) + 32;

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Temperature conversion",
                Description = $"🌡 {(double)Cel}°C is equal to {(double)Fah}°F.",
                //Description = $"📶 Reply: `{(int)sw.Elapsed.TotalMilliseconds}ms`",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}