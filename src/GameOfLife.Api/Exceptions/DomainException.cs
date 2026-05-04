namespace GameOfLife.Api.Exceptions;

/// <summary>
/// Base exception for business rule violations in the Game of Life domain.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Creates a new domain exception with a user-safe message.
    /// </summary>
    public DomainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a new domain exception with an inner exception for diagnostics.
    /// </summary>
    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
