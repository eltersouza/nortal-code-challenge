using FluentAssertions;
using GameOfLife.Api.Domain.Models;
using GameOfLife.Api.Infrastructure.Data;

namespace GameOfLife.UnitTests.Infrastructure;

public class BoardStateStorageAdapterTests
{
    [Fact]
    public void SerializeAndDeserialize_ShouldPreserveBoardShapeAndGeneration()
    {
        // Arrange
        var cells = new bool[3, 3];
        cells[0, 1] = true;
        cells[1, 1] = true;
        cells[2, 1] = true;
        var state = new BoardState(cells, generation: 7);

        // Act
        var json = BoardStateStorageAdapter.Serialize(state);
        var restoredState = BoardStateStorageAdapter.Deserialize(json);

        // Assert
        restoredState.Generation.Should().Be(7);
        restoredState.Hash.Should().Be(state.Hash);
        restoredState.Cells.Should().BeEquivalentTo(cells);
    }
}
