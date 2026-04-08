namespace CompVault.Frontend.Common.Configuration;

public sealed class AuthSettings
{
    public const string SectionName = "Auth";

    /// <summary>
    /// Levetid på auth-cookie i nettleseren
    /// Bør matche JwtSettings:RefreshTokenDays i backend
    /// </summary>
    public int CookieExpireDays { get; init; } = 7;

    /// <summary>
    /// Hvor ofte brukeren valideres mot backend
    /// En deaktivert bruker kan være innlogget i opptil dette antallet minutter
    /// </summary>
    public int ValidationIntervalMinutes { get; init; } = 10;
}