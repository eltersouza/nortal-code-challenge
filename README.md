# Conway's Game of Life API

API em C# com .NET 9 para o code challenge da Nortal. O projeto expõe endpoints para:
- criar um tabuleiro e retornar seu ID;
- calcular a próxima geração;
- calcular o estado após `N` gerações;
- encontrar o estado final estável ou retornar erro quando o limite de tentativas é excedido.

## Stack

- ASP.NET Core Web API
- Entity Framework Core + SQLite
- Serilog
- xUnit + FluentAssertions + Moq
- Docker

## Como está organizado

- `src/GameOfLife.Api/Controllers`: endpoints HTTP
- `src/GameOfLife.Api/Services`: regras de aplicação
- `src/GameOfLife.Api/Domain`: motor do jogo e modelos
- `src/GameOfLife.Api/Infrastructure`: persistência e mapeamento EF Core
- `tests`: testes unitários e de integração

## Persistência

Os boards são persistidos em SQLite. Isso permite que a aplicação reinicie ou caia sem perder os estados já enviados.

- Local: `src/GameOfLife.Api/gameoflife.db`
- Docker: `/app/data/gameoflife.db`

## Executando localmente

Pré-requisito: .NET 9 SDK

```bash
dotnet restore
dotnet run --project src/GameOfLife.Api
```

Em desenvolvimento, a API sobe com Swagger habilitado. Pelas configurações atuais de `launchSettings.json`, a URL HTTP padrão é `http://localhost:5208`.

## Executando com Docker

```bash
docker compose up --build
```

A API fica disponível em `http://localhost:8080`.

## Testes

```bash
dotnet test GameOfLife.sln
```

Ou separadamente:

```bash
dotnet test tests/GameOfLife.UnitTests/GameOfLife.UnitTests.csproj
dotnet test tests/GameOfLife.IntegrationTests/GameOfLife.IntegrationTests.csproj
```

## Endpoints principais

- `POST /api/boards`
- `GET /api/boards/{id}`
- `GET /api/boards/{id}/next`
- `GET /api/boards/{id}/states/{generations}`
- `GET /api/boards/{id}/final?maxGenerations=1000`
- `GET /health`

## Assunções

- O board é imutável após a criação.
- As bordas são tratadas como células mortas.
- Os estados futuros são recalculados sob demanda a partir do estado inicial persistido.
- O estado final considera tanto padrões estáveis quanto ciclos detectados por repetição de hash.
