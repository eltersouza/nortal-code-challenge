# Conway's Game of Life API

C# and .NET 9 API built for the Nortal code challenge. The project exposes endpoints to:
- create a board and return its ID;
- calculate the next generation;
- calculate the board state after `N` generations;
- find the final stable state or return an error when the attempt limit is exceeded.

## Stack

- ASP.NET Core Web API
- Entity Framework Core + SQLite
- Serilog
- xUnit + FluentAssertions + Moq
- Docker

## Project Structure

- `src/GameOfLife.Api/Controllers`: HTTP endpoints
- `src/GameOfLife.Api/Services`: application logic
- `src/GameOfLife.Api/Domain`: game engine and domain models
- `src/GameOfLife.Api/Infrastructure`: persistence and EF Core mapping
- `tests`: unit and integration tests

## Persistence

Boards are persisted in SQLite so the application can restart or crash without losing previously uploaded board states.

- Local: `src/GameOfLife.Api/gameoflife.db`
- Docker: `/app/data/gameoflife.db`

## Run Locally

Prerequisite: .NET 9 SDK

```bash
dotnet restore
dotnet run --project src/GameOfLife.Api
```

In development, Swagger is enabled. Based on the current `launchSettings.json`, the default HTTP URL is `http://localhost:5208`.

## Run with Docker

```bash
docker compose up --build
```

The API is available at `http://localhost:8080`.

## Tests

```bash
dotnet test GameOfLife.sln
```

Or individually:

```bash
dotnet test tests/GameOfLife.UnitTests/GameOfLife.UnitTests.csproj
dotnet test tests/GameOfLife.IntegrationTests/GameOfLife.IntegrationTests.csproj
```

## Main Endpoints

- `POST /api/boards`
- `GET /api/boards/{id}`
- `GET /api/boards/{id}/next`
- `GET /api/boards/{id}/states/{generations}`
- `GET /api/boards/{id}/final?maxGenerations=1000`
- `GET /health`

## Assumptions

- The board is immutable after creation.
- Board edges are treated as dead cells.
- Future states are recalculated on demand from the persisted initial state.
- The final state check considers both stable patterns and cycles detected through repeated hashes.
