using GameOfLife.Api.DTOs;
using GameOfLife.Api.Options;
using GameOfLife.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace GameOfLife.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BoardsController : ControllerBase
{
    private readonly IGameOfLifeService _service;
    private readonly GameOfLifeOptions _options;

    /// <summary>
    /// Creates the controller with access to the application service and runtime configuration.
    /// </summary>
    public BoardsController(
        IGameOfLifeService service,
        IOptions<GameOfLifeOptions> options)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Creates a new board with the initial state
    /// </summary>
    /// <param name="request">The initial board state</param>
    /// <returns>The created board ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreateBoardResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreateBoardResponse>> CreateBoard([FromBody] CreateBoardRequest request)
    {
        var response = await _service.CreateBoardAsync(ConvertToGrid(request.Cells));

        return CreatedAtAction(
            nameof(GetBoard),
            new { id = response.Id },
            response);
    }

    /// <summary>
    /// Gets the board as it was originally created
    /// </summary>
    /// <param name="id">The board ID</param>
    /// <returns>The stored initial board state</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BoardStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardStateDto>> GetBoard(Guid id)
    {
        var board = await _service.GetBoardAsync(id);
        return Ok(board);
    }

    /// <summary>
    /// Gets the next state of the board
    /// </summary>
    /// <param name="id">The board ID</param>
    /// <returns>The next board state</returns>
    [HttpGet("{id}/next")]
    [ProducesResponseType(typeof(BoardStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardStateDto>> GetNextState(Guid id)
    {
        var state = await _service.GetNextStateAsync(id);
        return Ok(state);
    }

    /// <summary>
    /// Gets the board state after a specified number of generations
    /// </summary>
    /// <param name="id">The board ID</param>
    /// <param name="generations">Number of generations to advance</param>
    /// <returns>The board state after the specified generations</returns>
    [HttpGet("{id}/states/{generations}")]
    [ProducesResponseType(typeof(BoardStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BoardStateDto>> GetStateAfterGenerations(Guid id, int generations)
    {
        var state = await _service.GetStateAfterGenerationsAsync(id, generations);
        return Ok(state);
    }

    /// <summary>
    /// Gets the final stable state of the board
    /// </summary>
    /// <param name="id">The board ID</param>
    /// <param name="maxGenerations">Maximum number of generations to calculate (defaults to configuration)</param>
    /// <returns>The final stable board state</returns>
    [HttpGet("{id}/final")]
    [ProducesResponseType(typeof(BoardStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<BoardStateDto>> GetFinalState(
        Guid id,
        [FromQuery] int? maxGenerations = null)
    {
        var state = await _service.GetFinalStateAsync(
            id,
            maxGenerations ?? _options.DefaultMaxGenerations);
        return Ok(state);
    }

    /// <summary>
    /// Converts the request payload from a jagged array into the multidimensional grid used internally.
    /// </summary>
    private static bool[,] ConvertToGrid(bool[][] rows)
    {
        int rowCount = rows.Length;
        int columnCount = rows[0].Length;
        var grid = new bool[rowCount, columnCount];

        for (int row = 0; row < rowCount; row++)
        {
            for (int column = 0; column < columnCount; column++)
            {
                grid[row, column] = rows[row][column];
            }
        }

        return grid;
    }
}
