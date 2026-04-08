using CompVault.Backend.Common.Responses;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Diagnostics;
namespace CompVault.Backend.Common.Middleware;

/// <summary>
/// Fanger opp alle ukjente exceptions og returnerer en ryddig JSON-feilmelding
/// i stedet for en stygg stack trace til klienten.
/// </summary>
internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    /// <summary>
    /// Fanger opp uhåndterte exceptions, logger dem og returnerer en strukturert
    /// <see cref="ProblemDetail"/>-respons med riktig HTTP-statuskode og <see cref="ErrorCode"/>.
    /// </summary>
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Uhåndtert exception på {Method} {Path}",
            httpContext.Request.Method, httpContext.Request.Path);

        ProblemDetail problem = ProblemDetailBuilder.FromException(exception);

        httpContext.Response.StatusCode = problem.Status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}