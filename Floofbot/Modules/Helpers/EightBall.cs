using System;
using System.Collections.Generic;

namespace Floofbot.Modules.Helpers
{
    public static class EightBall
    {
        private static readonly List<string> RESPONSES = new List<string> {
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

        public static string GetRandomResponse() {
            Random random = new Random();
            int randomNumber = random.Next(RESPONSES.Count);
            return RESPONSES[randomNumber];
        }
    }
}