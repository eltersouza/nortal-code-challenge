using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using GameOfLife.Api.DTOs;
using Microsoft.AspNetCore.Mvc.Testing;

namespace GameOfLife.IntegrationTests;

public class BoardsControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BoardsControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBoard_ValidRequest_ShouldReturn201Created()
    {
        // Arrange
        var request = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { true, false, true },
                new[] { false, true, false },
                new[] { true, false, true }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<CreateBoardResponse>();
        result.Should().NotBeNull();
        result!.Id.Should().NotBeEmpty();
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.AbsolutePath
            .Should().BeEquivalentTo($"/api/boards/{result.Id}");
    }

    [Fact]
    public async Task CreateBoard_EmptyRequest_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateBoardRequest
        {
            Cells = Array.Empty<bool[]>()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateBoard_NonRectangularBoard_ShouldReturn400BadRequest()
    {
        // Arrange
        var request = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { true, false, true },
                new[] { false, true }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/boards", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBoard_ExistingBoard_ShouldReturn200Ok()
    {
        // Arrange
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { true, false, false },
                new[] { false, true, false },
                new[] { false, false, true }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        result.Should().NotBeNull();
        result!.Generation.Should().Be(0);
        result.IsStable.Should().BeFalse();
        result.Cells[0][0].Should().BeTrue();
        result.Cells[1][1].Should().BeTrue();
        result.Cells[2][2].Should().BeTrue();
    }

    [Fact]
    public async Task GetNextState_ExistingBoard_ShouldReturn200OK()
    {
        // Arrange - Create a board first
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { false, true, false },
                new[] { false, true, false },
                new[] { false, true, false }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/next");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        result.Should().NotBeNull();
        result!.Generation.Should().Be(1);
        result.Cells.Should().NotBeNull();
    }

    [Fact]
    public async Task GetNextState_NonExistingBoard_ShouldReturn404NotFound()
    {
        // Arrange
        var nonExistingId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/boards/{nonExistingId}/next");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetStateAfterGenerations_ValidRequest_ShouldReturn200OK()
    {
        // Arrange - Create a board
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { true, true, false },
                new[] { true, false, false },
                new[] { false, false, false }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/states/5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        result.Should().NotBeNull();
        result!.Generation.Should().Be(5);
    }

    [Fact]
    public async Task GetStateAfterGenerations_NegativeGenerations_ShouldReturn400BadRequest()
    {
        // Arrange
        var boardId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/boards/{boardId}/states/-1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetFinalState_StillLife_ShouldReturn200OK()
    {
        // Arrange - Create a block (still life)
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { false, false, false, false },
                new[] { false, true, true, false },
                new[] { false, true, true, false },
                new[] { false, false, false, false }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/final?maxGenerations=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        result.Should().NotBeNull();
        result!.IsStable.Should().BeTrue();
        result.Generation.Should().BeLessThan(100);
    }

    [Fact]
    public async Task GetFinalState_Oscillator_ShouldDetectCycle()
    {
        // Arrange - Create a blinker (oscillator)
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, false, false, false }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/final?maxGenerations=100");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<BoardStateDto>();
        result.Should().NotBeNull();
        result!.IsStable.Should().BeTrue();
    }

    [Fact]
    public async Task GetFinalState_NoConvergence_ShouldReturn422UnprocessableEntity()
    {
        // Arrange - Create a glider (moves, doesn't stabilize in small space)
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, true, false, false, false, false, false, false, false },
                new[] { false, false, false, true, false, false, false, false, false, false },
                new[] { false, true, true, true, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false },
                new[] { false, false, false, false, false, false, false, false, false, false }
            }
        };

        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act - Use a very small maxGenerations to force timeout
        var response = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/final?maxGenerations=5");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturn200Healthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompleteWorkflow_CreateAndEvolveBlinker_ShouldWorkCorrectly()
    {
        // Arrange - Create a blinker
        var createRequest = new CreateBoardRequest
        {
            Cells = new[]
            {
                new[] { false, false, false, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, true, false, false },
                new[] { false, false, false, false, false }
            }
        };

        // Act 1: Create board
        var createResponse = await _client.PostAsJsonAsync("/api/boards", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBoard = await createResponse.Content.ReadFromJsonAsync<CreateBoardResponse>();

        // Act 2: Get next state
        var nextResponse = await _client.GetAsync($"/api/boards/{createdBoard!.Id}/next");
        nextResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var nextState = await nextResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        // Act 3: Get state after 2 generations (should be back to original)
        var twoGensResponse = await _client.GetAsync($"/api/boards/{createdBoard.Id}/states/2");
        twoGensResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var twoGensState = await twoGensResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        // Act 4: Get final state
        var finalResponse = await _client.GetAsync($"/api/boards/{createdBoard.Id}/final?maxGenerations=10");
        finalResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var finalState = await finalResponse.Content.ReadFromJsonAsync<BoardStateDto>();

        // Assert
        nextState!.Generation.Should().Be(1);
        twoGensState!.Generation.Should().Be(2);
        finalState!.IsStable.Should().BeTrue();
        finalState.Generation.Should().BeLessThanOrEqualTo(10);
    }
}
