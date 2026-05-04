using System.ComponentModel.DataAnnotations;

namespace GameOfLife.Api.Options;

/// <summary>
/// Runtime configuration for board size and generation limits.
/// </summary>
public class GameOfLifeOptions
{
    public const string SectionName = "GameOfLife";

    [Range(1, 10_000)]
    public int MaxBoardSize { get; init; } = 1_000;

    [Range(1, 100_000)]
    public int MaxGenerations { get; init; } = 10_000;

    [Range(1, 100_000)]
    public int DefaultMaxGenerations { get; init; } = 1_000;
}
