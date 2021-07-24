/* using System;
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
            Regex fahReg = new Regex("\\b(\\d+)(f)\\b", RegexOptions.IgnoreCase);
            Regex celReg = new Regex("\\b(\\d+)(c)\\b", RegexOptions.IgnoreCase);
            Regex miReg = new Regex("\\b(\\d+)(mi)\\b", RegexOptions.IgnoreCase);
            Regex kmReg = new Regex("\\b(\\d+)(km)\\b", RegexOptions.IgnoreCase);
            Regex kgReg = new Regex("\\b(\\d+)(kg)\\b", RegexOptions.IgnoreCase);
            Regex lbReg = new Regex("\\b(\\d+)(lbs)\\b", RegexOptions.IgnoreCase);

            if (fahReg.Match(input).Success) {
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
            else if (celReg.Match(input).Success) {
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
            else if (miReg.Match(input).Success) {
                Match m = miReg.Match(input);

                Group g = m.Groups[1];

                string miStr = Convert.ToString(g);
                double miTmp = Convert.ToDouble(miStr);

                Length mi = Length.FromMiles(miTmp);
                double km = mi.Kilometers;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Length conversion",
                    Description = $"📏 {(Length)mi} is equal to {(double)Math.Round(km, 3, MidpointRounding.ToEven)}Km.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (kmReg.Match(input).Success) {
                Match m = kmReg.Match(input);

                Group g = m.Groups[1];

                string kmStr = Convert.ToString(g);
                double kmTmp = Convert.ToDouble(kmStr);

                Length km = Length.FromKilometers(kmTmp);
                double mi = km.Miles;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Length conversion",
                    Description = $"📏 {(Length)km} is equal to {(double)Math.Round(mi, 3, MidpointRounding.ToEven)}mi.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (kgReg.Match(input).Success) {
                Match m = kgReg.Match(input);

                Group g = m.Groups[1];

                string kgStr = Convert.ToString(g);
                double kgTmp = Convert.ToDouble(kgStr);


                Mass kg = Mass.FromKilograms(kgTmp);
                double lb = kg.Pounds;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Mass conversion",
                    Description = $"⚖️ {(Mass)kg} is equal to {(double)Math.Round(lb, 3, MidpointRounding.ToEven)}lbs.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else if (lbReg.Match(input).Success) {
                Match m = lbReg.Match(input);

                Group g = m.Groups[1];

                string lbStr = Convert.ToString(g);
                double lbTmp = Convert.ToDouble(lbStr);

                Mass lb = Mass.FromPounds(lbTmp);
                double kg = lb.Kilograms;

                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Mass conversion",
                    Description = $"⚖️ {(Mass)lb} is equal to {(double)Math.Round(kg, 3, MidpointRounding.ToEven)}Kg.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
            else {
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Conversion module",
                    Description = $"No unit has been entered, or it was not recognized. Available units are mi<->km, °C<->F, and kg<->lbs.",
                    Color = EMBED_COLOR
                };

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
        }
    }
}
*/
