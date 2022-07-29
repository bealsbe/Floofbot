using System;
using System.Collections.Generic;

namespace Floofbot.Modules.Helpers
{
    public class MinesweeperBoard
    {
        private static readonly string BOMB_SQUARE = "||:bomb:||";
        private readonly string[,] _grid;
        private readonly int _height;
        private readonly int _width;
        private readonly int _bombCount;

        public MinesweeperBoard(int height, int width, int bombCount)
        {
            _height = height;
            _width = width;
            _bombCount = bombCount;
            _grid = new string[_height, _width];
            
            PlantBombs();
            FillBoardNumbers();
        }

        private void PlantBombs()
        {
            var rnd = new Random();
            
            for (int i = 0; i < _bombCount; i++)
            {
                int randX;
                int randY;
                
                do
                {
                    randX = rnd.Next(_height);
                    randY = rnd.Next(_width);
                }
                while (_grid[randX, randY] == BOMB_SQUARE);
                
                _grid[randX, randY] = BOMB_SQUARE;
            }
        }

        private void FillBoardNumbers()
        {
            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (_grid[i, j] != BOMB_SQUARE)
                    {
                        _grid[i, j] = CountNeighbouringBombs(i, j).ToString();
                    }
                }
            }
        }

        private int CountNeighbouringBombs(int x, int y)
        {
            var count = 0;
            
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (!(i == 0 && j == 0)
                        && x + i >= 0 && x + i < _grid.GetLength(0)
                        && y + j >= 0 && y + j < _grid.GetLength(1)
                        && _grid[x + i, y + j] == BOMB_SQUARE)
                    {
                        count++;
                    }
                }
            }
            
            return count;
        }

        public override string ToString()
        {
            var board = string.Empty;
            
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

            for (int i = 0; i < _height; i++)
            {
                for (int j = 0; j < _width; j++)
                {
                    if (reponse.ContainsKey(_grid[i, j]))
                        board += reponse[_grid[i, j]];
                    else
                        board += _grid[i, j];
                }
                board += "\n";
            }
            
            return board;
        }
    }
}
