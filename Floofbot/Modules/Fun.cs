using Discord.Commands;
using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Floofbot.Modules
{
    public class fun : ModuleBase<SocketCommandContext>
    {
        [Command("8ball")]
        [Summary("Ask the Magic 8-Ball a question")]
        public async Task eightball([Summary("question")][Remainder] string question)
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
            int randomNumber = random.Next(0, 19);
            EmbedBuilder builder = new EmbedBuilder();
            builder.Title = "Magic 8 Ball";
            builder.AddField("Question", question);
            builder.AddField("Answer", responses[randomNumber]);
            builder.Color = Color.DarkOrange;
            await Context.Channel.SendMessageAsync("", false, builder.Build());
        }
    }
}
