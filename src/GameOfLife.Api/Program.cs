using GameOfLife.Api.Domain.Logic;
using GameOfLife.Api.Infrastructure.Data;
using GameOfLife.Api.Infrastructure.Data.Repositories;
using GameOfLife.Api.Middleware;
using GameOfLife.Api.Options;
using GameOfLife.Api.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/gameoflife-.txt",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddOptions<GameOfLifeOptions>()
    .Bind(builder.Configuration.GetSection(GameOfLifeOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(
        options => options.DefaultMaxGenerations <= options.MaxGenerations,
        $"{GameOfLifeOptions.SectionName}:DefaultMaxGenerations must be less than or equal to MaxGenerations.")
    .ValidateOnStart();

// Database
builder.Services.AddDbContext<GameOfLifeDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gameoflife.db"));

// Domain services (Singleton - stateless)
builder.Services.AddSingleton<GameOfLifeEngine>();

// Application services (Scoped - per request)
builder.Services.AddScoped<IGameOfLifeService, GameOfLifeService>();

// Infrastructure (Scoped - per request)
builder.Services.AddScoped<IBoardRepository, BoardRepository>();

// Exception handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<GameOfLifeDbContext>();

var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GameOfLifeDbContext>();
    db.Database.Migrate();
    Log.Information("Database migrations applied");
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Game of Life API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

Log.Information("Conway's Game of Life API starting...");

try
{
    app.Run();
    Log.Information("Application stopped gracefully");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }
