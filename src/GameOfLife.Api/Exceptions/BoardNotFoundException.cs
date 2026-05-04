namespace GameOfLife.Api.Exceptions;

/// <summary>
/// Thrown when the requested board ID does not exist in persistence.
/// </summary>
public class BoardNotFoundException : DomainException
{
    public Guid BoardId { get; }

    /// <summary>
    /// Creates an exception bound to the missing board identifier.
    /// </summary>
    public BoardNotFoundException(Guid boardId)
        : base($"Board {boardId} not found")
    {
        BoardId = boardId;
    }
}
