using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TicketSystem.Infrastructure;

/// <summary>
/// Turns a unique index violation into 409 Conflict instead of letting it surface as a 500.
/// Endpoints check for duplicates up front, but a concurrent request can still slip past that
/// check and reach the database constraint, which is the real guarantee.
/// </summary>
public sealed class UniqueConstraintExceptionHandler : IExceptionHandler
{
    private const int UniqueIndexViolation = 2601;
    private const int UniqueConstraintViolation = 2627;

    private readonly ILogger<UniqueConstraintExceptionHandler> _logger;

    public UniqueConstraintExceptionHandler(ILogger<UniqueConstraintExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateException dbUpdateException
            || dbUpdateException.InnerException is not SqlException sqlException
            || (sqlException.Number != UniqueIndexViolation
                && sqlException.Number != UniqueConstraintViolation))
        {
            return false;
        }

        _logger.LogWarning(
            dbUpdateException,
            "A unique constraint rejected the write to {Path}",
            httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "Duplicate value",
            Detail = "A record with the same unique value already exists.",
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;
        await httpContext.Response.WriteAsJsonAsync(
            problem,
            options: null,
            contentType: "application/problem+json",
            cancellationToken);

        return true;
    }
}
