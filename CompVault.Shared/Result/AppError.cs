namespace CompVault.Shared.Result;

/// <summary>
/// En strukturert feil med kode og melding. Brukes i stedet for å kaste exceptions
/// for forventet forretningslogikk, f.eks. "bruker finnes ikke".
/// </summary>
public sealed class AppError
{
    /// <summary>Feilkoden.</summary>
    public ErrorCode Code { get; }

    /// <summary>En lesbar feilmelding.</summary>
    public string Message { get; }

    /// <summary>Valgfrie felt-spesifikke valideringsfeil, f.eks. fra skjemavalidering.</summary>
    public IReadOnlyDictionary<string, string[]>? Details { get; }

    private AppError(ErrorCode code, string message, IReadOnlyDictionary<string, string[]>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }

    /// <summary>Lager en <see cref="AppError"/> med valgfri kode og melding.</summary>
    public static AppError Create(ErrorCode code, string message) =>
        new(code, message);

    /// <summary>Lager en valideringsfeil med felt-spesifikke detaljer.</summary>
    public static AppError Validation(string message, IReadOnlyDictionary<string, string[]> details) =>
        new(ErrorCode.Validation, message, details);

    /// <summary>Lager en "ikke funnet"-feil.</summary>
    public static AppError NotFound(string message) =>
        new(ErrorCode.NotFound, message);

    /// <summary>Lager en "ikke logget inn"-feil.</summary>
    public static AppError Unauthorized(string message = "Unauthorized.") =>
        new(ErrorCode.Unauthorized, message);

    /// <summary>Lager en konfliktfeil, f.eks. når en e-post allerede er i bruk.</summary>
    public static AppError Conflict(string message) =>
        new(ErrorCode.Conflict, message);

    /// <inheritdoc />
    public override string ToString() => $"[{Code}] {Message}";
}
