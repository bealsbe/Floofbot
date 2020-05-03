using System;
using System.Collections.Generic;
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
            // A lowercase version of the input required
            // to get a potential response from the bot
            string loweredRequiredInput = response.Input.ToLower();
            if ((response.RequireExact && loweredMessageContent == loweredRequiredInput)
                || loweredMessageContent.Contains(loweredRequiredInput))
            {
                if (val < response.Probability)
                {
                    return response.Response;
                }
            }
        }

        return string.Empty;
    }
}
