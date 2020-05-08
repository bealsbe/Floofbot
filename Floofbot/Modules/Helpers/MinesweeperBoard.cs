using System;
using System.Collections.Generic;

namespace Floofbot.Modules.Helpers
{
    public class MinesweeperBoard
    {
        private static readonly string BOMB_SQUARE = "||:bomb:||";
        private string[,] grid;
        private int height;
        private int width;
        private int bombCount;

        public MinesweeperBoard(int height, int width, int bombCount)
        {
            this.height = height;
            this.width = width;
            this.bombCount = bombCount;
            grid = new string[this.height, this.width];
            PlantBombs();
            FillBoardNumbers();
        }

        private void PlantBombs()
        {
            Random rnd = new Random();
            for (int i = 0; i < bombCount; i++)
            {
                int randX;
                int randY;
                do
                {
                    randX = rnd.Next(height);
                    randY = rnd.Next(width);
                }
                while (grid[randX, randY] == BOMB_SQUARE);
                grid[randX, randY] = BOMB_SQUARE;
            }
        }

        private void FillBoardNumbers()
        {
            for (int i = 0; i < this.height; i++)
            {
                for (int j = 0; j < this.width; j++)
                {
                    if (grid[i, j] != BOMB_SQUARE)
                    {
                        grid[i, j] = CountNeighbouringBombs(i, j).ToString();
                    }
                }
            }
        }

        private int CountNeighbouringBombs(int x, int y)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (!(i == 0 && j == 0)
                        && x + i >= 0 && x + i < grid.GetLength(0)
                        && y + j >= 0 && y + j < grid.GetLength(1)
                        && grid[x + i, y + j] == BOMB_SQUARE)
                    {
                        count++;
                    }
                }
            }
            return count;
        }

        public override string ToString()
        {
            string board = "";
            var reponse = new Dictionary<string, string> {
                { "0" ,"||:zero:||" },
                { "1" ,"||:one:||" },
                { "2" ,"||:two:||" },
                { "3" ,"||:three:||" },
                { "4" ,"||:four:||" },
                { "5" ,"||:five:||" },
                { "6" ,"||:six:||" },
                { "7" ,"||:seven:||" },
                { "8" ,"||:eight:||" },
            };

            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    if (reponse.ContainsKey(grid[i, j]))
                        board += reponse[grid[i, j]];
                    else
                        board += grid[i, j];
                }
                board += "\n";
            }
            return board;
        }
    }
}
