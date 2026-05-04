namespace GameOfLife.Api.DTOs;

/// <summary>
/// Response returned after a board is successfully created.
/// </summary>
public class CreateBoardResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
}
