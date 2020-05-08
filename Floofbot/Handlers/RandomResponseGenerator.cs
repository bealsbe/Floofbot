using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using Floofbot.Configs;

class RandomResponseGenerator
{
    public static string GenerateResponse(SocketUserMessage userMessage)
    {
        List<BotRandomResponse> responses = BotConfigFactory.Config.RandomResponses;
        if (responses == null || responses.Count == 0)
        {
            return string.Empty;
        }

        Random rand = new Random();
        double val = rand.NextDouble();
        foreach (var response in responses)
        {
            Regex requiredInput = new Regex(response.Input, RegexOptions.IgnoreCase);
            Match match = requiredInput.Match(userMessage.Content);
            if (match.Success && val < response.Probability)
            {
                if (match.Groups.Count == 1) {
                    // no regex needed for the output
                    return response.Response;
                }

                List<string> matchedValues = new List<string>(match.Groups.Count - 1);
                for (int i = 1; i < match.Groups.Count; i++) 
                {
                    matchedValues.Add(match.Groups[i].Value);
                }
                return string.Format(response.Response, matchedValues.ToArray());
            }
        }
        return string.Empty;
    }
}
