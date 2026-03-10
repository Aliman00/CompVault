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

        var (status, code, message) = exception switch
        {
            ArgumentException argEx         => (400, ErrorCode.Validation, argEx.Message),
            KeyNotFoundException            => (404, ErrorCode.NotFound, "Ressursen ble ikke funnet."),
            UnauthorizedAccessException     => (403, ErrorCode.Forbidden, "Du har ikke tilgang."),
            NotImplementedException         => (501, ErrorCode.Unknown, "Denne funksjonen er ikke tilgjengelig ennå."),
            OperationCanceledException      => (499, ErrorCode.Unknown, "Forespørselen ble avbrutt."),
            _                               => (500, ErrorCode.Unknown, "Noe gikk galt på vår side." +
                                                                        " Prøv igjen litt senere.")
        };

        var problem = new ProblemDetail
        {
            Status  = status,
            Code    = code.ToString(),
            Message = message
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
