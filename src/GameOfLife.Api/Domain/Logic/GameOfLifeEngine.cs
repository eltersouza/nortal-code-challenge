namespace GameOfLife.Api.Domain.Logic;

/// <summary>
/// Applies Conway's Game of Life rules to a finite two-dimensional board.
/// </summary>
public class GameOfLifeEngine
{
    /// <summary>
    /// Calculates the next generation for the provided board without mutating the input.
    /// </summary>
    /// <param name="cells">Current board state where <see langword="true"/> represents a live cell.</param>
    /// <returns>A new grid containing the next generation.</returns>
    public bool[,] CalculateNextGeneration(bool[,] cells)
    {
        if (cells == null)
            throw new ArgumentNullException(nameof(cells));

        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        bool[,] nextGeneration = new bool[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int liveNeighbors = CountLiveNeighbors(cells, x, y);
                nextGeneration[x, y] = ApplyRules(cells[x, y], liveNeighbors);
            }
        }

        return nextGeneration;
    }

    /// <summary>
    /// Counts the live neighbors around a single cell, considering out-of-bounds positions as dead.
    /// </summary>
    private int CountLiveNeighbors(bool[,] cells, int x, int y)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        int count = 0;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                    continue;

                int nx = x + dx;
                int ny = y + dy;

                if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                {
                    if (cells[nx, ny])
                        count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Applies the four Conway rules to a cell using its current state and live neighbor count.
    /// </summary>
    private bool ApplyRules(bool isAlive, int liveNeighbors)
    {
        if (isAlive)
        {
            // Rule 1: Death by underpopulation
            if (liveNeighbors < 2)
                return false;

            // Rule 2: Survival
            if (liveNeighbors == 2 || liveNeighbors == 3)
                return true;

            // Rule 3: Death by overpopulation
            if (liveNeighbors > 3)
                return false;
        }
        else
        {
            // Rule 4: Birth
            if (liveNeighbors == 3)
                return true;
        }

        return false;
    }
}
