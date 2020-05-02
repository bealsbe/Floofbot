using System;
using Discord.WebSocket;

class RandomResponseGenerator {
    private static readonly double AWOO_PROBABILITY = 0.1;
    private static readonly double OWO_PROBABILITY = 0.025;
    private static readonly double UWU_PROBABILITY = 0.025;
    private static readonly double LOL_PROBABILITY = 0.25;

    public string generateResponse(SocketUserMessage userMessage) {
        string loweredMessageContent = userMessage.Content.ToLower();
        Random rand = new Random();
        double val = rand.NextDouble();

        if (val < AWOO_PROBABILITY
            && loweredMessageContent.Contains("awoo")) {
            return "Legalize Awoo!";
        }
        if (val < OWO_PROBABILITY
            && loweredMessageContent.Contains("owo")) {
            return "UwU";
        }
        if (val < UWU_PROBABILITY
            && loweredMessageContent.Contains("uwu")) {
            return "OwO";
        }
        if (val < LOL_PROBABILITY
            && loweredMessageContent == "lol") {
            return "lol";
        }

        return string.Empty;
    }
}
