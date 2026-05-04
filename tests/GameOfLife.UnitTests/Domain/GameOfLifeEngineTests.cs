using FluentAssertions;
using GameOfLife.Api.Domain.Logic;

namespace GameOfLife.UnitTests.Domain;

public class GameOfLifeEngineTests
{
    private readonly GameOfLifeEngine _engine;

    public GameOfLifeEngineTests()
    {
        _engine = new GameOfLifeEngine();
    }

    [Fact]
    public void CalculateNextGeneration_EmptyBoard_ShouldRemainEmpty()
    {
        // Arrange
        var emptyBoard = new bool[3, 3];

        // Act
        var result = _engine.CalculateNextGeneration(emptyBoard);

        // Assert
        result.Should().BeEquivalentTo(emptyBoard);
    }

    [Fact]
    public void CalculateNextGeneration_SingleCell_ShouldDie()
    {
        // Arrange
        var board = new bool[3, 3];
        board[1, 1] = true; // Single cell in center

        // Act
        var result = _engine.CalculateNextGeneration(board);

        // Assert
        result.Should().BeEquivalentTo(new bool[3, 3]); // All dead
    }

    [Fact]
    public void CalculateNextGeneration_Block_ShouldRemainStable()
    {
        // Arrange - Block (2x2 still life)
        var block = new bool[4, 4];
        block[1, 1] = true;
        block[1, 2] = true;
        block[2, 1] = true;
        block[2, 2] = true;

        // Act
        var result = _engine.CalculateNextGeneration(block);

        // Assert
        result.Should().BeEquivalentTo(block);
    }

    [Fact]
    public void CalculateNextGeneration_Blinker_ShouldOscillate()
    {
        // Arrange - Blinker vertical
        var blinkerVertical = new bool[5, 5];
        blinkerVertical[1, 2] = true;
        blinkerVertical[2, 2] = true;
        blinkerVertical[3, 2] = true;

        // Expected - Blinker horizontal
        var blinkerHorizontal = new bool[5, 5];
        blinkerHorizontal[2, 1] = true;
        blinkerHorizontal[2, 2] = true;
        blinkerHorizontal[2, 3] = true;

        // Act
        var result = _engine.CalculateNextGeneration(blinkerVertical);

        // Assert
        result.Should().BeEquivalentTo(blinkerHorizontal);
    }

    [Fact]
    public void CalculateNextGeneration_Blinker_ShouldOscillateBack()
    {
        // Arrange - Blinker horizontal
        var blinkerHorizontal = new bool[5, 5];
        blinkerHorizontal[2, 1] = true;
        blinkerHorizontal[2, 2] = true;
        blinkerHorizontal[2, 3] = true;

        // Expected - Back to vertical
        var blinkerVertical = new bool[5, 5];
        blinkerVertical[1, 2] = true;
        blinkerVertical[2, 2] = true;
        blinkerVertical[3, 2] = true;

        // Act
        var result = _engine.CalculateNextGeneration(blinkerHorizontal);

        // Assert
        result.Should().BeEquivalentTo(blinkerVertical);
    }

    [Fact]
    public void CalculateNextGeneration_Glider_ShouldMoveCorrectly()
    {
        // Arrange - Glider initial position
        var glider = new bool[6, 6];
        glider[1, 2] = true;
        glider[2, 3] = true;
        glider[3, 1] = true;
        glider[3, 2] = true;
        glider[3, 3] = true;

        // Act - Calculate next generation
        var result = _engine.CalculateNextGeneration(glider);

        // Assert - Should have specific pattern
        result[2, 1].Should().BeTrue();
        result[2, 3].Should().BeTrue();
        result[3, 2].Should().BeTrue();
        result[3, 3].Should().BeTrue();
        result[4, 2].Should().BeTrue();
    }

    [Fact]
    public void CalculateNextGeneration_Underpopulation_CellDies()
    {
        // Arrange - Cell with only 1 neighbor
        var board = new bool[3, 3];
        board[1, 1] = true; // Center cell
        board[0, 0] = true; // One neighbor

        // Act
        var result = _engine.CalculateNextGeneration(board);

        // Assert - Both should die (underpopulation)
        result[1, 1].Should().BeFalse();
    }

    [Fact]
    public void CalculateNextGeneration_Overpopulation_CellDies()
    {
        // Arrange - Cell with 4 neighbors (overpopulation)
        var board = new bool[3, 3];
        board[1, 1] = true; // Center cell
        board[0, 0] = true;
        board[0, 1] = true;
        board[0, 2] = true;
        board[1, 0] = true;

        // Act
        var result = _engine.CalculateNextGeneration(board);

        // Assert - Center should die (overpopulation)
        result[1, 1].Should().BeFalse();
    }

    [Fact]
    public void CalculateNextGeneration_Birth_DeadCellBecomesAlive()
    {
        // Arrange - Three live cells around a dead center
        var board = new bool[3, 3];
        board[0, 0] = true;
        board[0, 1] = true;
        board[1, 0] = true;
        // Center [1,1] is dead

        // Act
        var result = _engine.CalculateNextGeneration(board);

        // Assert - Center should be born
        result[1, 1].Should().BeTrue();
    }

    [Fact]
    public void CalculateNextGeneration_NullInput_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => _engine.CalculateNextGeneration(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
