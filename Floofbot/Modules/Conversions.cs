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

        /*[Command("temp")]
        [Summary("Converts a temperature. Arguments are `[unit]` and `[temperature]`.")]
        public async Task Temp(string unit, double input)
        {
            if(unit == "F" || unit == "f")
            else if(celReg.Match(input).Success)
            {
                Temperature Fah = Temperature.FromDegreesFahrenheit(input);
                double Cel = Fah.DegreesCelsius;
                
                EmbedBuilder builder = new EmbedBuilder()
                {
                    Title = "Temperature conversion",
                    Description=$"🌡 {(Temperature)Fah} is equal to {(double)Math.Round(Cel,2, MidpointRounding.ToEven)}°C.",
                    Color = EMBED_COLOR
                };
                Match m = celReg.Match(input);

                await Context.Channel.SendMessageAsync("", false, builder.Build());
            }
                Group g = m.Groups[1];

            else if(unit == "C" || unit == "c")
            {
                Temperature Cel = Temperature.FromDegreesCelsius(input);
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
        [Command("length")]
        [Summary("In progress")]
        public async Task Length(double input)
        {
            double baseLgt = input / 2.54;
            double ftLgt = Math.Floor(baseLgt / 12);
            double inLgt = baseLgt - (12 * ftLgt);

            EmbedBuilder builder = new EmbedBuilder()
            {
                Title = "Length conversion",
                Description = $"📏 {(double)input}cm is equal to {(double)ftLgt}\"{(double)inLgt}\'.",
                Color = EMBED_COLOR
            };

            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }*/
    }
}