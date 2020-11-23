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
            if (Regex.Match(input, "\\b(\\d+)(f)\\b", RegexOptions.IgnoreCase).Success)
            {
                Match m = Regex.Match(input, "\\b(\\d+)(f)\\b", RegexOptions.IgnoreCase);

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
            else if(Regex.Match(input, "\\b(\\d+)(c)\\b", RegexOptions.IgnoreCase).Success)
            {
                Match m = Regex.Match(input, "\\b(\\d+)(c)\\b", RegexOptions.IgnoreCase);

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
        }
    }
}