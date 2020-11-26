using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using UnitsNet;

namespace Floofbot
{
    [Summary("Conversion commands")]
    [Discord.Commands.Name("Conversions")]
    public class Conversions : InteractiveBase
    {
        private static readonly Discord.Color EMBED_COLOR = Color.Magenta;

        [Command("convert")]
        [Alias("conv")]
        [Summary("Converts units to other units, such as Celcius to Fahrenheit.")]

        public async Task convert(string input)
        {
            string fahPat = "\\b(\\d+)(f)\\b";
            Regex fahReg = new Regex(fahPat, RegexOptions.IgnoreCase);
            string celPat = "\\b(\\d+)(c)\\b";
            Regex celReg = new Regex(celPat, RegexOptions.IgnoreCase);
            string miPat = "\\b(\\d+)(mi)\\b";
            Regex miReg = new Regex(miPat, RegexOptions.IgnoreCase);

            if (fahReg.Match(input).Success)
            {
                Match m = fahReg.Match(input);

                Group g = m.Groups[1];

                string FahStr = Convert.ToString(g);
                double FahTmp = Convert.ToDouble(FahStr);

                Temperature Fah = Temperature.FromDegreesFahrenheit(FahTmp);
                double Cel = Fah.DegreesCelsius;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description = $"🌡 {(Temperature)Fah} is equal to {(double)Math.Round(Cel, 2, MidpointRounding.ToEven)}°C.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if(celReg.Match(input).Success)
            {
                Match m = celReg.Match(input);

                Group g = m.Groups[1];

                string celStr = Convert.ToString(g);
                double celTmp = Convert.ToDouble(celStr);

                Temperature Cel = Temperature.FromDegreesCelsius(celTmp);
                double Fah = Cel.DegreesFahrenheit;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description = $"🌡 {(Temperature)Cel} is equal to {(double)Math.Round(Fah, 2, MidpointRounding.ToEven)}°F.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (miReg.Match(input).Success)
            {
                Match m = miReg.Match(input);

                Group g = m.Groups[1];

                string miStr = Convert.ToString(g);
                double miTmp = Convert.ToDouble(miStr);

                Length mi = Length.FromMiles(miTmp);
                double km = mi.Kilometers;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description = $"📏 {(Length)mi} is equal to {(double)Math.Round(km, 3, MidpointRounding.ToEven)}Km.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }
    }
}