using CompVault.Shared.Result;
using Microsoft.AspNetCore.Mvc;

namespace CompVault.Backend.Common.Controller;

/// <summary>
/// Basekontroller alle kontrollere arver fra for å samle logikk
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Oversetter et mislykket <see cref="Result{T}"/> til en HTTP-feilrespons med <see cref="ProblemDetail"/>.
    /// </summary>
    /// <param name="result">Et mislykket <see cref="Result{T}"/>-objekt som inneholder feilen som skal håndteres.</param>
    /// <typeparam name="T">Typen til verdien i <see cref="Result{T}"/>.</typeparam>
    /// <returns>En <see cref="ActionResult"/> med riktig HTTP-statuskode og en <see cref="ProblemDetail"/>-respons.</returns>
    /// <exception cref="InvalidOperationException">Kastes dersom <paramref name="result"/> er vellykket.</exception>
    protected ActionResult HandleFailure<T>(Result<T> result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Kan ikke håndtere feil for et vellykket resultat.");

        return BuildErrorResponse(result.Error!);
    }

    /// <summary>
    /// Oversetter et mislykket <see cref="Result"/> til en HTTP-feilrespons med <see cref="ProblemDetail"/>.
    /// </summary>
    /// <param name="result">Et mislykket <see cref="Result"/>-objekt som inneholder feilen som skal håndteres.</param>
    /// <returns>En <see cref="ActionResult"/> med riktig HTTP-statuskode og en <see cref="ProblemDetail"/>-respons.</returns>
    /// <exception cref="InvalidOperationException">Kastes dersom <paramref name="result"/> er vellykket.</exception>
    protected ActionResult HandleFailure(Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Kan ikke håndtere feil for et vellykket resultat.");

        return BuildErrorResponse(result.Error!);
    }

    /// <summary>
    /// Bygger en <see cref="ActionResult"/> med riktig HTTP-statuskode og <see cref="ProblemDetail"/>
    /// basert på <see cref="ErrorCode"/> i den gitte <see cref="AppError"/>.
    /// </summary>
    /// <param name="error">Feilen som skal oversettes til en HTTP-feilrespons.</param>
    /// <returns>En <see cref="ActionResult"/> med tilhørende <see cref="ProblemDetail"/>-respons.</returns>
    private ActionResult BuildErrorResponse(AppError error)
    {
        var statusCode = error.Code switch
        {
            ErrorCode.NotFound => 404,
            ErrorCode.UserNotFound => 404,
            ErrorCode.Conflict => 409,
            ErrorCode.UserAlreadyExists => 409,
            ErrorCode.Unauthorized => 401,
            ErrorCode.InvalidCredentials => 401,
            ErrorCode.TokenExpired => 401,
            ErrorCode.InvalidToken => 401,
            ErrorCode.Forbidden => 403,
            ErrorCode.AccountLocked => 403,
            ErrorCode.AccountInactive => 403,
            ErrorCode.EmailNotConfirmed => 403,
            ErrorCode.EmailSendFailed => 500,
            ErrorCode.Validation => 422,
            ErrorCode.PasswordTooWeak => 422,
            ErrorCode.OtpMaxAttemptsExceeded => 429,
            ErrorCode.OtpCooldown => 429,
            ErrorCode.OtpInvalidOrExpired => 401,
            _ => 400
        };

        return StatusCode(statusCode, new ProblemDetail
        {
            Status = statusCode,
            Code = error.Code.ToString(),
            Message = error.Message
        });
    }
}