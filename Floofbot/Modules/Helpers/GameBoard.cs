using System;
using System.Collections.Generic;
using System.Text;

namespace Floofbot.Modules.Helpers
{
    public class GameBoard
    {
        private string[,] grid;
        private int height;
        private int width;
        private int bombCount;

        public GameBoard(int height, int width, int bombCount)
        {
            this.height = height;
            this.width = width;
            this.bombCount = bombCount;
            grid = new string[this.height, this.width];
            plantBombs();

            for (int i = 0; i < this.height; i++)
            {
                for (int j = 0; j < this.width; j++)
                {
                    if (grid[i, j] != "||:bomb:||")
                    {
                        grid[i, j] = getBombCount(i, j).ToString();
                    }
                }
            }
        }

        public int getBombCount(int x, int y)
        {
            int count = 0;

            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (!(i == 0 && j == 0))
                    {
                        //checks to see if the array goes out of bounds
                        if (!(x + i < 0 || y + j < 0 || x + i >= grid.GetLength(0) || y + j >= grid.GetLength(1)))
                        {
                            if (grid[x + i, y + j] == "||:bomb:||")
                            {
                                count++;
                            }
                        }
                    }
                }
            }
            return count;
        }

        public void plantBombs()
        {
            Random rnd = new Random();

            for (int i = 0; i < bombCount; i++)
            {
                bool isGood = false;
                while (!isGood)
                {
                    int randX = rnd.Next(0, height);
                    int randY = rnd.Next(0, width);

                    if (grid[randX, randY] != "||:bomb:||")
                    {
                        isGood = true;
                        grid[randX, randY] = "||:bomb:||";
                    }
                }
            }
        }
        public string getBoard()
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
                    if (reponse.ContainsKey(grid[i,j]))
                        board += reponse[grid[i,j]];
                    else
                        board += grid[i, j];
                }
                board += "\n";
            }
            return board;
        }

    }
}
