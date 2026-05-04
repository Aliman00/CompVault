using CompVault.Shared.Result;

namespace CompVault.Backend.Common.Responses;

/// <summary>
/// Sentraliserer opprettelse av ProblemDetail-objekter for konsistent feilhåndtering.
/// </summary>
public static class ProblemDetailBuilder
{
    /// <summary>
    /// Oppretter et ProblemDetail basert på en AppError.
    /// </summary>
    public static ProblemDetail FromError(AppError error)
    {
        int statusCode = GetStatusCode(error.Code);
        string message = error.Message;

        return new ProblemDetail
        {
            Status = statusCode,
            Code = error.Code.ToString(),
            Message = message
        };
    }

    /// <summary>
    /// Oppretter et ProblemDetail basert på en Exception.
    /// </summary>
    public static ProblemDetail FromException(Exception exception)
    {
        (int status, ErrorCode code, string message) = exception switch
        {
            ArgumentException argEx => (400, ErrorCode.Validation, argEx.Message),
            KeyNotFoundException => (404, ErrorCode.NotFound, GetDefaultMessage(ErrorCode.NotFound)),
            UnauthorizedAccessException => (403, ErrorCode.Forbidden, GetDefaultMessage(ErrorCode.Forbidden)),
            NotImplementedException => (501, ErrorCode.Unknown, "Denne funksjonen er ikke tilgjengelig ennå."),
            OperationCanceledException => (499, ErrorCode.Unknown, "Forespørselen ble avbrutt."),
            _ => (500, ErrorCode.Unknown, GetDefaultMessage(ErrorCode.InternalError))
        };

        return new ProblemDetail
        {
            Status = status,
            Code = code.ToString(),
            Message = message
        };
    }

    /// <summary>
    /// Oppretter et ProblemDetail for en gitt statuskode og melding.
    /// </summary>
    public static ProblemDetail Create(int statusCode, string code, string? message = null)
    {
        return new ProblemDetail
        {
            Status = statusCode,
            Code = code,
            Message = message ?? GetDefaultMessageByStatus(statusCode)
        };
    }

    /// <summary>
    /// Mapper ErrorCode til HTTP-statuskode.
    /// </summary>
    public static int GetStatusCode(ErrorCode code) => code switch
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
        ErrorCode.InternalError => 500,
        _ => 400
    };

    /// <summary>
    /// Henter standard melding for en ErrorCode.
    /// </summary>
    public static string GetDefaultMessage(ErrorCode code) => code switch
    {
        ErrorCode.NotFound => "Ressursen ble ikke funnet.",
        ErrorCode.UserNotFound => "Brukeren ble ikke funnet.",
        ErrorCode.Conflict => "En konflikt oppstod med ressursen.",
        ErrorCode.UserAlreadyExists => "En bruker med denne e-posten eksisterer allerede.",
        ErrorCode.Unauthorized => "Du er ikke autorisert til å utføre denne handlingen.",
        ErrorCode.InvalidCredentials => "Ugyldige påloggingsdetaljer.",
        ErrorCode.TokenExpired => "Tokenen har utløpt.",
        ErrorCode.InvalidToken => "Tokenen er ugyldig.",
        ErrorCode.Forbidden => "Du har ikke tilgang.",
        ErrorCode.AccountLocked => "Kontoen din er låst.",
        ErrorCode.AccountInactive => "Kontoen din er inaktiv.",
        ErrorCode.EmailNotConfirmed => "E-posten din er ikke bekreftet.",
        ErrorCode.EmailSendFailed => "Kunne ikke sende e-post. Prøv igjen senere.",
        ErrorCode.Validation => "En eller flere valideringer feilet.",
        ErrorCode.PasswordTooWeak => "Passordet oppfyller ikke kravene.",
        ErrorCode.OtpMaxAttemptsExceeded => "For mange feilede forsøk. Prøv igjen senere.",
        ErrorCode.OtpCooldown => "Vennligst vent før du ber om en ny kode.",
        ErrorCode.OtpInvalidOrExpired => "Koden er ugyldig eller har utløpt.",
        ErrorCode.InternalError => "Noe gikk galt på vår side. Prøv igjen litt senere.",
        _ => "Noe gikk galt på vår side. Prøv igjen litt senere."
    };

    private static string GetDefaultMessageByStatus(int statusCode) => statusCode switch
    {
        400 => "Forespørselen var ugyldig.",
        401 => GetDefaultMessage(ErrorCode.Unauthorized),
        403 => GetDefaultMessage(ErrorCode.Forbidden),
        404 => GetDefaultMessage(ErrorCode.NotFound),
        409 => GetDefaultMessage(ErrorCode.Conflict),
        422 => GetDefaultMessage(ErrorCode.Validation),
        429 => "For mange forespørsler. Prøv igjen senere.",
        500 => "Noe gikk galt på vår side. Prøv igjen litt senere.",
        _ => "Noe gikk galt på vår side. Prøv igjen litt senere."
    };
}