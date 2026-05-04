using FluentAssertions;
using GameOfLife.Api.Domain.Logic;
using GameOfLife.Api.Domain.Models;
using GameOfLife.Api.Exceptions;
using GameOfLife.Api.Infrastructure.Data.Repositories;
using GameOfLife.Api.Options;
using GameOfLife.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace GameOfLife.UnitTests.Services;

public class GameOfLifeServiceTests
{
    private const int MaxBoardSize = 10;
    private const int MaxGenerations = 25;

    private readonly Mock<IBoardRepository> _mockRepository;
    private readonly GameOfLifeEngine _engine;
    private readonly Mock<ILogger<GameOfLifeService>> _mockLogger;
    private readonly GameOfLifeService _service;

    public GameOfLifeServiceTests()
    {
        _mockRepository = new Mock<IBoardRepository>();
        _engine = new GameOfLifeEngine();
        _mockLogger = new Mock<ILogger<GameOfLifeService>>();
        _service = new GameOfLifeService(
            _mockRepository.Object,
            _engine,
            _mockLogger.Object,
            Options.Create(new GameOfLifeOptions
            {
                MaxBoardSize = MaxBoardSize,
                MaxGenerations = MaxGenerations,
                DefaultMaxGenerations = 10
            }));
    }

    [Fact]
    public async Task CreateBoardAsync_ValidCells_ShouldReturnBoardId()
    {
        // Arrange
        var cells = new bool[3, 3];
        cells[1, 1] = true;

        // Act
        var result = await _service.CreateBoardAsync(cells);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Board>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateBoardAsync_BoardLargerThanConfiguredLimit_ShouldThrowDomainException()
    {
        // Arrange
        var oversizedBoard = new bool[MaxBoardSize + 1, 1];

        // Act
        Func<Task> act = async () => await _service.CreateBoardAsync(oversizedBoard);

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage($"Board size exceeds maximum limit of {MaxBoardSize}x{MaxBoardSize}");
    }

    [Fact]
    public async Task GetBoardAsync_ExistingBoard_ShouldReturnInitialState()
    {
        // Arrange
        var cells = new bool[3, 3];
        cells[1, 1] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        var result = await _service.GetBoardAsync(boardId);

        // Assert
        result.Id.Should().Be(boardId);
        result.Generation.Should().Be(0);
        result.IsStable.Should().BeFalse();
        result.Cells[1][1].Should().BeTrue();
    }

    [Fact]
    public async Task GetNextStateAsync_ExistingBoard_ShouldReturnNextState()
    {
        // Arrange
        var cells = new bool[3, 3];
        cells[1, 0] = true;
        cells[1, 1] = true;
        cells[1, 2] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        var result = await _service.GetNextStateAsync(boardId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(boardId);
        result.Generation.Should().Be(1);
        result.Cells.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNextStateAsync_NonExistingBoard_ShouldThrowBoardNotFoundException()
    {
        // Arrange
        var boardId = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync((Board?)null);

        // Act
        Func<Task> act = async () => await _service.GetNextStateAsync(boardId);

        // Assert
        await act.Should().ThrowAsync<BoardNotFoundException>()
            .WithMessage($"Board {boardId} not found");
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_ValidGenerations_ShouldReturnCorrectState()
    {
        // Arrange
        var cells = new bool[3, 3];
        cells[1, 1] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        var result = await _service.GetStateAfterGenerationsAsync(boardId, 5);

        // Assert
        result.Should().NotBeNull();
        result.Generation.Should().Be(5);
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_NegativeGenerations_ShouldThrowArgumentException()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _service.GetStateAfterGenerationsAsync(boardId, -1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("Generations must be >= 0*");
    }

    [Fact]
    public async Task GetStateAfterGenerationsAsync_TooManyGenerations_ShouldThrowArgumentException()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _service.GetStateAfterGenerationsAsync(boardId, MaxGenerations + 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage($"Generations must be <= {MaxGenerations}*");
    }

    [Fact]
    public async Task GetFinalStateAsync_StillLife_ShouldDetectStability()
    {
        // Arrange - Block (still life)
        var cells = new bool[4, 4];
        cells[1, 1] = true;
        cells[1, 2] = true;
        cells[2, 1] = true;
        cells[2, 2] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        var result = await _service.GetFinalStateAsync(boardId, MaxGenerations);

        // Assert
        result.Should().NotBeNull();
        result.IsStable.Should().BeTrue();
        result.Generation.Should().BeLessThan(MaxGenerations);
    }

    [Fact]
    public async Task GetFinalStateAsync_Oscillator_ShouldDetectCycle()
    {
        // Arrange - Blinker (oscillator with period 2)
        var cells = new bool[5, 5];
        cells[1, 2] = true;
        cells[2, 2] = true;
        cells[3, 2] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        var result = await _service.GetFinalStateAsync(boardId, MaxGenerations);

        // Assert
        result.Should().NotBeNull();
        result.IsStable.Should().BeTrue();
        result.Generation.Should().BeLessThan(MaxGenerations);
    }

    [Fact]
    public async Task GetFinalStateAsync_NoConvergence_ShouldThrowMaxGenerationsExceededException()
    {
        // Arrange - Create a pattern that doesn't stabilize quickly
        var cells = new bool[10, 10];
        // Create a glider or complex pattern
        cells[1, 2] = true;
        cells[2, 3] = true;
        cells[3, 1] = true;
        cells[3, 2] = true;
        cells[3, 3] = true;

        var board = Board.Create(cells, MaxBoardSize);
        var boardId = board.Id;

        _mockRepository.Setup(r => r.GetByIdAsync(boardId))
            .ReturnsAsync(board);

        // Act
        Func<Task> act = async () => await _service.GetFinalStateAsync(boardId, 10);

        // Assert
        await act.Should().ThrowAsync<MaxGenerationsExceededException>()
            .Where(ex => ex.BoardId == boardId && ex.MaxGenerations == 10);
    }

    [Fact]
    public async Task GetFinalStateAsync_ZeroMaxGenerations_ShouldThrowArgumentException()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _service.GetFinalStateAsync(boardId, 0);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage("Max generations must be > 0*");
    }

    [Fact]
    public async Task GetFinalStateAsync_TooManyMaxGenerations_ShouldThrowArgumentException()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _service.GetFinalStateAsync(boardId, MaxGenerations + 1);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessage($"Max generations must be <= {MaxGenerations}*");
    }
}
