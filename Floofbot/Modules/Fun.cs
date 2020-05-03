using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Fun commands")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private static readonly Discord.Color EMBED_COLOR = Color.DarkOrange;
        private static readonly int MAX_NUM_DICE = 20;
        private static readonly int MAX_NUM_SIDES = 1000;

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question")]
        public async Task AskEightBall([Summary("question")][Remainder] string question)
        {
            var responses = new List<string> {
                 "As I see it, yes.",
                 "Ask again later.",
                 "Better not tell you now.",
                 "Cannot predict now.",
                 "Concentrate and ask again.",
                 "Don’t count on it.",
                 "It is certain.",
                 "It is decidedly so.",
                 "Most likely.",
                 "My reply is no.",
                 "My sources say no.",
                 "Outlook not so good.",
                 "Outlook good.",
                 "Reply hazy, try again.",
                 "Signs point to yes.",
                 "Very doubtful.",
                 "Without a doubt.",
                 "Yes.",
                 "Yes – definitely.",
                 "You may rely on it."
            };
            Random random = new Random();
            int randomNumber = random.Next(responses.Count);
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Magic 8 Ball";
            builder.AddField("Question", question);
            builder.AddField("Answer", responses[randomNumber]);
            builder.Color = EMBED_COLOR;
            await SendEmbed(builder.Build());
        }

        [Command("xkcd")]
        [Summary("Get an xkcd comic by ID. If no ID given, get the latest one.")]
        public async Task xkcd([Summary("Comic ID")] string comicId = "")
        {
            int parsedComicId;
            if (!int.TryParse(comicId, out parsedComicId) || parsedComicId <= 0)
            {
                await Context.Channel.SendMessageAsync("Comic ID must be a positive integer less than or equal to Int32.MaxValue.");
                return;
            }

            string json;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    json = wc.DownloadString(new Uri($"https://xkcd.com/{comicId}/info.0.json"));
                }
                catch (Exception)
                {
                    await Context.Channel.SendMessageAsync("404 Not Found");
                    return;
                }
            }

            string imgLink;
            string imgHoverText;
            string comicTitle;
            using (JsonDocument parsedJson = JsonDocument.Parse(json))
            {
                imgLink = parsedJson.RootElement.GetProperty("img").ToString();
                imgHoverText = parsedJson.RootElement.GetProperty("alt").ToString();
                comicTitle = parsedJson.RootElement.GetProperty("safe_title").ToString();
            }

            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = comicTitle;
            builder.WithImageUrl(imgLink);
            builder.WithFooter(imgHoverText);
            builder.Color = EMBED_COLOR;
            await SendEmbed(builder.Build());
        }

        private Task SendEmbed(Embed embed)
        {
            return Context.Channel.SendMessageAsync("", false, embed);
        }

        [Command("roll")]
        [Summary("Roll some dice e.g. 1d20")]
        public async Task RollDice([Summary("dice")] string diceStr)
        {
            var match = Regex.Match(diceStr, @"^(?<numDice>\d+)?[dD](?<numSides>\d+)$",
                RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

            int numDice = 1;
            int numSides = 0;

            if (!match.Success)
            {
                await Context.Channel.SendMessageAsync("Dice rolled must be in a format such as 1d20, or d5");
            }
            else if (!int.TryParse(match.Groups["numSides"].Value, out numSides) || numSides > MAX_NUM_SIDES)
            {
                await Context.Channel.SendMessageAsync($"Each dice can have at most {MAX_NUM_SIDES} sides.");
            }
            else if (match.Groups["numDice"].Success &&
                (!int.TryParse(match.Groups["numDice"].Value, out numDice) || numDice > MAX_NUM_DICE))
            {
                await Context.Channel.SendMessageAsync($"At most {MAX_NUM_DICE} dice can be rolled at once.");
            }
            else if (numDice == 0)
            {
                await Context.Channel.SendMessageAsync($"At least one dice must be rolled.");
            }
            else if (numSides == 0)
            {
                await Context.Channel.SendMessageAsync($"Each dice must have at least one side.");
            }
            else
            {
                Random random = new Random();
                List<int> rolls = new List<int>(numDice);
                for (int i = 0; i < numDice; i++)
                {
                    rolls.Add(random.Next(numSides) + 1);
                }

                await Context.Channel.SendMessageAsync(string.Join(" ", rolls));
            }
        }

        [Command("catfact")]
        [Summary("Responds with a random cat fact")]
        public async Task RequestCatFact()
        {
            string json;
            using (WebClient wc = new WebClient())
            {
                try
                {
                    json = wc.DownloadString("https://catfact.ninja/fact");
                }
                catch (Exception)
                {
                    await Context.Channel.SendMessageAsync("The catfact command is currently unavailable.");
                    return;
                }
            }
            string fact;
            using (JsonDocument jsonDocument = JsonDocument.Parse(json))
            {
                fact = jsonDocument.RootElement.GetProperty("fact").ToString();
            }
            await Context.Channel.SendMessageAsync(fact);
        }
    }
}
