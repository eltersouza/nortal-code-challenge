using GameOfLife.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GameOfLife.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into consistent HTTP problem responses.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Creates the exception handler with access to logging and environment information.
    /// </summary>
    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Maps known application exceptions to HTTP status codes and serializes a <see cref="ProblemDetails"/> response.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            BoardNotFoundException ex => (
                StatusCodes.Status404NotFound,
                "Board not found",
                $"Board with ID {ex.BoardId} does not exist"
            ),
            MaxGenerationsExceededException ex => (
                StatusCodes.Status422UnprocessableEntity,
                "Max generations exceeded",
                $"Board did not reach stable state after {ex.MaxGenerations} generations"
            ),
            DomainException ex => (
                StatusCodes.Status400BadRequest,
                "Domain error",
                ex.Message
            ),
            ArgumentException ex => (
                StatusCodes.Status400BadRequest,
                "Invalid argument",
                ex.Message
            ),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal server error",
                "An unexpected error occurred"
            )
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        // Include stack trace only in Development
        if (_environment.IsDevelopment() && statusCode == StatusCodes.Status500InternalServerError)
        {
            problemDetails.Extensions["exception"] = exception.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
