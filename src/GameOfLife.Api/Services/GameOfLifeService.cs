using GameOfLife.Api.Domain.Logic;
using GameOfLife.Api.Domain.Models;
using GameOfLife.Api.DTOs;
using GameOfLife.Api.Exceptions;
using GameOfLife.Api.Infrastructure.Data.Repositories;
using GameOfLife.Api.Options;
using Microsoft.Extensions.Options;

namespace GameOfLife.Api.Services;

/// <summary>
/// Coordinates persistence, validation and Game of Life calculations for the API use cases.
/// </summary>
public class GameOfLifeService : IGameOfLifeService
{
    private readonly IBoardRepository _repository;
    private readonly GameOfLifeEngine _engine;
    private readonly ILogger<GameOfLifeService> _logger;
    private readonly GameOfLifeOptions _options;

    /// <summary>
    /// Creates the application service with its persistence, domain logic and configuration dependencies.
    /// </summary>
    public GameOfLifeService(
        IBoardRepository repository,
        GameOfLifeEngine engine,
        ILogger<GameOfLifeService> logger,
        IOptions<GameOfLifeOptions> options)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Persists a new board after validating it against the configured maximum size.
    /// </summary>
    public async Task<CreateBoardResponse> CreateBoardAsync(bool[,] initialCells)
    {
        var board = Board.Create(initialCells, _options.MaxBoardSize);
        await _repository.AddAsync(board);
        await _repository.SaveChangesAsync();

        _logger.LogInformation(
            "Board {BoardId} created with size {Width}x{Height}",
            board.Id, board.Width, board.Height);

        return new CreateBoardResponse
        {
            Id = board.Id,
            CreatedAt = board.CreatedAt
        };
    }

    /// <summary>
    /// Returns the original state that was uploaded for the specified board.
    /// </summary>
    public async Task<BoardStateDto> GetBoardAsync(Guid boardId)
    {
        var board = await GetBoardOrThrowAsync(boardId);
        return MapBoardState(boardId, board.InitialState, isStable: false);
    }

    /// <summary>
    /// Calculates the next generation for the specified board without mutating persisted state.
    /// </summary>
    public async Task<BoardStateDto> GetNextStateAsync(Guid boardId)
    {
        var board = await GetBoardOrThrowAsync(boardId);

        var nextState = new BoardState(
            _engine.CalculateNextGeneration(board.InitialState.Cells),
            board.InitialState.Generation + 1);

        return MapBoardState(boardId, nextState, isStable: false);
    }

    /// <summary>
    /// Replays the game for a fixed number of generations starting from the persisted initial state.
    /// </summary>
    public async Task<BoardStateDto> GetStateAfterGenerationsAsync(Guid boardId, int generations)
    {
        EnsureGenerationLimit(generations, nameof(generations), "Generations", allowZero: true);
        var board = await GetBoardOrThrowAsync(boardId);

        var cells = board.InitialState.Cells;

        for (int i = 0; i < generations; i++)
        {
            cells = _engine.CalculateNextGeneration(cells);
        }

        return MapBoardState(
            boardId,
            new BoardState(cells, board.InitialState.Generation + generations),
            isStable: false);
    }

    /// <summary>
    /// Searches for a repeated or stable state up to the provided generation limit.
    /// </summary>
    public async Task<BoardStateDto> GetFinalStateAsync(Guid boardId, int maxGenerations)
    {
        EnsureGenerationLimit(maxGenerations, nameof(maxGenerations), "Max generations", allowZero: false);
        var board = await GetBoardOrThrowAsync(boardId);

        var cells = board.InitialState.Cells;
        var stateHashes = new HashSet<string>();

        for (int gen = 0; gen < maxGenerations; gen++)
        {
            var currentState = new BoardState(cells, gen);

            // Detect still life or oscillator
            if (stateHashes.Contains(currentState.Hash))
            {
                _logger.LogInformation(
                    "Board {BoardId} reached stable state at generation {Generation}",
                    boardId, gen);

                return MapBoardState(boardId, currentState, isStable: true);
            }

            stateHashes.Add(currentState.Hash);
            cells = _engine.CalculateNextGeneration(cells);
        }

        _logger.LogWarning(
            "Board {BoardId} did not reach stable state after {MaxGenerations} generations",
            boardId, maxGenerations);

        throw new MaxGenerationsExceededException(boardId, maxGenerations);
    }

    /// <summary>
    /// Loads a board by ID or raises a domain exception when it does not exist.
    /// </summary>
    private async Task<Board> GetBoardOrThrowAsync(Guid boardId)
    {
        return await _repository.GetByIdAsync(boardId)
            ?? throw new BoardNotFoundException(boardId);
    }

    /// <summary>
    /// Validates a generation-based parameter against configured lower and upper bounds.
    /// </summary>
    private void EnsureGenerationLimit(int value, string parameterName, string label, bool allowZero)
    {
        if (allowZero)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName, $"{label} must be >= 0.");
        }
        else if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, $"{label} must be > 0.");
        }

        if (value > _options.MaxGenerations)
            throw new ArgumentOutOfRangeException(
                parameterName,
                $"{label} must be <= {_options.MaxGenerations}.");
    }

    /// <summary>
    /// Converts a domain board state into the DTO returned by the API.
    /// </summary>
    private static BoardStateDto MapBoardState(Guid boardId, BoardState state, bool isStable)
    {
        return new BoardStateDto
        {
            Id = boardId,
            Generation = state.Generation,
            Cells = ConvertTo2DArray(state.Cells),
            IsStable = isStable
        };
    }

    /// <summary>
    /// Converts a multidimensional grid into the jagged array shape exposed by the HTTP contract.
    /// </summary>
    private static bool[][] ConvertTo2DArray(bool[,] cells)
    {
        int width = cells.GetLength(0);
        int height = cells.GetLength(1);
        var result = new bool[width][];

        for (int i = 0; i < width; i++)
        {
            result[i] = new bool[height];
            for (int j = 0; j < height; j++)
            {
                result[i][j] = cells[i, j];
            }
        }

        return result;
    }
}
