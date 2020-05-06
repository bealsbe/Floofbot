using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Floofbot.Modules.Helpers
{
    class Dice
    {
        private static readonly int MAX_NUM_DICE = 20;
        private static readonly int MAX_NUM_SIDES = 1000;
        private int _numDice;
        private int _numSides;

        private Dice(int numDice, int numSides)
        {
            _numDice = numDice;
            _numSides = numSides;
        }

        public static Dice FromString(string diceStr)
        {
            var match = Regex.Match(diceStr, @"^(?<numDice>\d+)?[dD](?<numSides>\d+)$",
                RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.RightToLeft);

            int numDice = 1;
            int numSides;

            if (!match.Success)
            {
                throw new ArgumentException("Dice rolled must be in a format such as 1d20, or d5");
            }
            else if (!int.TryParse(match.Groups["numSides"].Value, out numSides) || numSides > MAX_NUM_SIDES)
            {
                throw new ArgumentException($"Each dice can have at most {MAX_NUM_SIDES} sides.");
            }
            else if (match.Groups["numDice"].Success &&
               (!int.TryParse(match.Groups["numDice"].Value, out numDice) || numDice > MAX_NUM_DICE))
            {
                throw new ArgumentException($"At most {MAX_NUM_DICE} dice can be rolled at once.");
            }
            else if (numDice == 0)
            {
                throw new ArgumentException($"At least one dice must be rolled.");
            }
            else if (numSides == 0)
            {
                throw new ArgumentException($"Each dice must have at least one side.");
            }
            return new Dice(numDice, numSides);
        }

        public List<int> GenerateRolls()
        {
            Random random = new Random();
            List<int> rolls = new List<int>(_numDice);
            for (int i = 0; i < _numDice; i++)
            {
                rolls.Add(random.Next(_numSides) + 1);
            }
            return rolls;
        }
    }
}