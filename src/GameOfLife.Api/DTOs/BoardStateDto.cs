namespace GameOfLife.Api.DTOs;

/// <summary>
/// Response model returned when the API exposes a board state for a specific generation.
/// </summary>
public class BoardStateDto
{
    public Guid Id { get; set; }
    public int Generation { get; set; }
    public bool[][] Cells { get; set; } = Array.Empty<bool[]>();
    public bool IsStable { get; set; }
}
