using GameOfLife.Api.DTOs;

namespace GameOfLife.Api.Services;

/// <summary>
/// Application service responsible for orchestrating board creation and state calculations.
/// </summary>
public interface IGameOfLifeService
{
    /// <summary>
    /// Creates and persists a new board from the uploaded seed state.
    /// </summary>
    Task<CreateBoardResponse> CreateBoardAsync(bool[,] initialCells);

    /// <summary>
    /// Returns the original persisted state of a board.
    /// </summary>
    Task<BoardStateDto> GetBoardAsync(Guid boardId);

    /// <summary>
    /// Calculates the immediate next generation for a board.
    /// </summary>
    Task<BoardStateDto> GetNextStateAsync(Guid boardId);

    /// <summary>
    /// Calculates the state of a board after a fixed number of generations.
    /// </summary>
    Task<BoardStateDto> GetStateAfterGenerationsAsync(Guid boardId, int generations);

    /// <summary>
    /// Finds the first repeated or stable state for a board within the supplied generation limit.
    /// </summary>
    Task<BoardStateDto> GetFinalStateAsync(Guid boardId, int maxGenerations);
}
