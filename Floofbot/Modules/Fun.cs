using Discord;
using Discord.Commands;
using Floofbot.Modules.Helpers;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    [Summary("Fun commands")]
    [Name("Fun")]
    public class Fun : ModuleBase<SocketCommandContext>
    {
        private static readonly Discord.Color EMBED_COLOR = Color.DarkOrange;
        private static Random rand = new Random();

        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question")]
        public async Task AskEightBall([Summary("question")][Remainder] string question)
        {
            EmbedBuilder builder = new EmbedBuilder
            {
                Title = "Magic 8 Ball",
                Color = EMBED_COLOR,
            };
            builder.AddField("Question", question);
            builder.AddField("Answer", EightBall.GetRandomResponse());
            await SendEmbed(builder.Build());
        }

        [Command("xkcd")]
        [Summary("Get an xkcd comic by ID. If no ID given, get the latest one.")]
        public async Task XKCD([Summary("Comic ID")] string comicId = "")
        {
            int parsedComicId;
            if ((!int.TryParse(comicId, out parsedComicId) || parsedComicId <= 0) && !String.IsNullOrEmpty(comicId))
            {
                await Context.Channel.SendMessageAsync("Comic ID must be a positive integer less than or equal to " + Int32.MaxValue + ".");
                return;
            }

            string json;
            if (parsedComicId == 0)
            {
                json = await ApiFetcher.RequestSiteContentAsString("https://xkcd.com/info.0.json");
            }
            else
            {
                json = await ApiFetcher.RequestSiteContentAsString($"https://xkcd.com/{comicId}/info.0.json");
            }

            if (string.IsNullOrEmpty(json))
            {
                await Context.Channel.SendMessageAsync("404 Not Found");
                return;
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

            EmbedBuilder builder = new EmbedBuilder
            {
                Title = comicTitle,
                Color = EMBED_COLOR,
            };
            builder.WithImageUrl(imgLink);
            builder.WithFooter(imgHoverText);
            await SendEmbed(builder.Build());
        }

        [Command("roll")]
        [Summary("Roll some dice e.g. 1d20")]
        public async Task RollDice([Summary("dice")] string diceStr = "")
        {
            try
            {
                Dice dice = Dice.FromString(diceStr);
                await Context.Channel.SendMessageAsync(string.Join(" ", dice.GenerateRolls()));
            }
            catch (ArgumentException e)
            {
                // This exception occurs when parsing the dice string,
                // and is meant to be displayed to the user
                // there is no need to log it
                await Context.Channel.SendMessageAsync(e.Message);
            }
        }

        [Command("catfact")]
        [Summary("Responds with a random cat fact")]
        public async Task RequestCatFact()
        {
            string fact = await ApiFetcher.RequestStringFromApi("https://catfact.ninja/fact", "fact");
            if (!string.IsNullOrEmpty(fact))
            {
                await Context.Channel.SendMessageAsync(fact);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The catfact command is currently unavailable.");
            }
        }

        [Command("foxfact")]
        [Summary("Responds with a random fox fact")]
        public async Task RequestFoxFact()
        {
            string fact = await ApiFetcher.RequestStringFromApi("https://some-random-api.ml/facts/fox", "fact");
            if (!string.IsNullOrEmpty(fact))
            {
                await Context.Channel.SendMessageAsync(fact);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The foxfact command is currently unavailable.");
            }
        }

        [Command("cat")]
        [Summary("Responds with a random cat")]
        public async Task RequestCat()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://aws.random.cat/meow", "file");
            if (!string.IsNullOrEmpty(fileUrl) && Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                await SendAnimalEmbed(":cat:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The cat command is currently unavailable.");
            }
        }

        [Command("dog")]
        [Summary("Responds with a random dog")]
        public async Task RequestDog()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://random.dog/woof.json", "url");
            if (!string.IsNullOrEmpty(fileUrl) && Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                await SendAnimalEmbed(":dog:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The dog command is currently unavailable.");
            }
        }

        [Command("fox")]
        [Summary("Responds with a random fox")]
        public async Task RequestFox()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://wohlsoft.ru/images/foxybot/randomfox.php", "file");
            if (!string.IsNullOrEmpty(fileUrl) && Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                await SendAnimalEmbed(":fox:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The fox command is currently unavailable.");
            }
        }

        [Command("birb")]
        [Summary("Responds with a random birb")]
        public async Task RequestBirb()
        {
            string fileUrl = await ApiFetcher.RequestEmbeddableUrlFromApi("https://random.birb.pw/tweet.json", "file");
            if (!string.IsNullOrEmpty(fileUrl) && Uri.IsWellFormedUriString(fileUrl, UriKind.Absolute))
            {
                fileUrl = "https://random.birb.pw/img/" + fileUrl;
                await SendAnimalEmbed(":bird:", fileUrl);
            }
            else
            {
                await Context.Channel.SendMessageAsync("The birb command is currently unavailable.");
            }
        }

        [Command("choice")]
        [Summary("Chooses one item from a list of possible options")]
        public async Task Choice([Summary("the options, delimited by ';'")][Remainder]string choices = "")
        {
            if (!string.IsNullOrEmpty(choices))
            {
                string[] splitChoices = choices.Split(";")
                    .Select(choice => choice.Trim())
                    .Where(choice => !string.IsNullOrEmpty(choice)).ToArray();
                if (splitChoices.Length == 1)
                {
                    await Context.Channel.SendMessageAsync("You need to give me more than one choice!");
                    return;
                }
                else if (splitChoices.Length != 0)
                {
                    EmbedBuilder choiceEmbed = new EmbedBuilder(){
                        Description = "Chosen choice: " + splitChoices[rand.Next(splitChoices.Length)],
                        Color = EMBED_COLOR
                    };

                    await Context.Channel.SendMessageAsync("", false, choiceEmbed.Build());
                    return;
                }
            }
            string usageString = "Not enough options were provided, or all options were whitespace.\n" +
                "Example usage: `.choice choiceA; choiceB; choiceC`";
            EmbedBuilder builder = new EmbedBuilder {
                Description = usageString,
                Color = EMBED_COLOR
            };
            await SendEmbed(builder.Build());
        }

        [Command("minesweeper")]
        [Summary("Minesweeper minigame")]
        public async Task Minesweeper([Summary("width")]int width, [Summary("height")]int height, [Summary("bomb count")]int bombs)
        {
            if (width < 1 || height < 1 || bombs < 0)
            {
                await Context.Channel.SendMessageAsync("Invalid grid size or bomb count");
            }
            else if (width > 10 || height > 10)
            {
                await Context.Channel.SendMessageAsync("Max Grid Size: 10 x 10");
            }
            else if (bombs >= height * width)
            {
                await Context.Channel.SendMessageAsync("Too many bombs!");
            }
            else
            {
                MinesweeperBoard game = new MinesweeperBoard(height, width, bombs);
                EmbedBuilder builder = new EmbedBuilder
                {
                    Title = ":bomb: Minesweeper",
                    Color = EMBED_COLOR,
                    Description = game.ToString()
                };
                await SendEmbed(builder.Build());
            }
        }

        private async Task SendAnimalEmbed(string title, string fileUrl)
        {
            EmbedBuilder builder = new EmbedBuilder()
                .WithTitle(title)
                .WithColor(EMBED_COLOR)
                .WithImageUrl(fileUrl);
            await SendEmbed(builder.Build());
        }

        private async Task SendEmbed(Embed embed)
        {
            await Context.Channel.SendMessageAsync("", false, embed);
        }
    }
}
