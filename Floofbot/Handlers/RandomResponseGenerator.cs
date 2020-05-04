using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Floofbot.Configs;

class RandomResponseGenerator
{
    public string generateResponse(SocketUserMessage userMessage)
    {
        List<BotRandomResponse> responses = BotConfigFactory.Config.RandomResponses;
        if (responses == null || responses.Count == 0)
        {
            return string.Empty;
        }

        string loweredMessageContent = userMessage.Content.ToLower();
        Random rand = new Random();
        double val = rand.NextDouble();

        foreach (var response in responses)
        {
            Regex requiredInput = new Regex(response.Input, RegexOptions.IgnoreCase);
            Match match = requiredInput.Match(loweredMessageContent);
            if (match.Success && val < response.Probability)
            {
                return response.Response;
            }
        }
        return string.Empty;
    }
}
