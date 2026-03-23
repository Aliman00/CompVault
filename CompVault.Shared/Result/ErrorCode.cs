namespace CompVault.Shared.Result;

/// <summary>
/// Alle feilkodene vi bruker i appen. Gjør det enkelt å skille feiltyper fra hverandre.
/// </summary>
public enum ErrorCode
{
    // Generelle feil
    Unknown = 0,
    Validation = 1000,
    NotFound = 1001,
    Conflict = 1002,
    Forbidden = 1003,
    Unauthorized = 1004,
    InternalError = 1005, // Unventede tekniske feil (exceptions, transactions, etc.)

    // Autentisering
    InvalidCredentials = 2000,
    AccountLocked = 2001,
    AccountInactive = 2002,
    TokenExpired = 2003,
    InvalidToken = 2004,
    EmailNotConfirmed = 2005,
    OtpInvalidOrExpired = 2006,
    OtpCooldown = 2008,
    OtpMaxAttemptsExceeded = 2009,

    // Brukere
    UserNotFound = 3000,
    UserAlreadyExists = 3001,
    PasswordTooWeak = 3002,

    // Epost
    EmailSendFailed = 4000,


    // Frontend
    NetworkError = 5000

}