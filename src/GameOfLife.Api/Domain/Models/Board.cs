using GameOfLife.Api.Exceptions;

namespace GameOfLife.Api.Domain.Models;

/// <summary>
/// Represents a persisted Game of Life board and its original uploaded state.
/// </summary>
public class Board
{
    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public int Width { get; private set; }
    public int Height { get; private set; }
    public BoardState InitialState { get; private set; } = null!;

    /// <summary>
    /// Constructor reserved for EF Core materialization.
    /// </summary>
    private Board()
    {
        // Constructor for EF Core
    }

    /// <summary>
    /// Creates a new board from the uploaded seed state and validates its dimensions.
    /// </summary>
    /// <param name="initialCells">Initial state uploaded by the client.</param>
    /// <param name="maxBoardSize">Maximum allowed width and height for a board.</param>
    /// <returns>A new <see cref="Board"/> ready to be persisted.</returns>
    public static Board Create(bool[,] initialCells, int maxBoardSize)
    {
        if (initialCells == null)
            throw new DomainException("Initial cells cannot be null");

        if (maxBoardSize <= 0)
            throw new DomainException("Maximum board size must be greater than 0");

        int width = initialCells.GetLength(0);
        int height = initialCells.GetLength(1);

        if (width == 0 || height == 0)
            throw new DomainException("Board dimensions must be greater than 0");

        if (width > maxBoardSize || height > maxBoardSize)
            throw new DomainException($"Board size exceeds maximum limit of {maxBoardSize}x{maxBoardSize}");

        var board = new Board
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Width = width,
            Height = height,
            InitialState = new BoardState(initialCells, 0)
        };

        return board;
    }
}
