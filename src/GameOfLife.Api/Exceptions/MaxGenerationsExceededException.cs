namespace GameOfLife.Api.Exceptions;

/// <summary>
/// Thrown when a board does not reach a repeated or stable state within the configured limit.
/// </summary>
public class MaxGenerationsExceededException : DomainException
{
    public Guid BoardId { get; }
    public int MaxGenerations { get; }

    /// <summary>
    /// Creates an exception that records the board ID and the attempt limit that was exceeded.
    /// </summary>
    public MaxGenerationsExceededException(Guid boardId, int maxGenerations)
        : base($"Board {boardId} did not reach final state after {maxGenerations} generations")
    {
        BoardId = boardId;
        MaxGenerations = maxGenerations;
    }
}
