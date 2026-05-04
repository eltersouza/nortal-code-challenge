# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/GameOfLife.Api/GameOfLife.Api.csproj", "src/GameOfLife.Api/"]
RUN dotnet restore "src/GameOfLife.Api/GameOfLife.Api.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/GameOfLife.Api"
RUN dotnet build "GameOfLife.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "GameOfLife.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final runtime image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN useradd -m -u 1000 appuser && \
    chown -R appuser:appuser /app

# Copy published app
COPY --from=publish /app/publish .

# Create directory for database and logs
RUN mkdir -p /app/data /app/logs && \
    chown -R appuser:appuser /app/data /app/logs

USER appuser

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "GameOfLife.Api.dll"]
