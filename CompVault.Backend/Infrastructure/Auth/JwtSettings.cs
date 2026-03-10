namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// JWT-innstillinger hentet fra appsettings.json. Bind automatisk til seksjonen "JwtSettings".
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>Hemmelig nøkkel brukt til å signere tokens. Hold denne privat!</summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>Hvem som utsteder tokenet (oss).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Hvem tokenet er ment for (klientene).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Hvor lenge access token er gyldig (minutter).</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Hvor lenge refresh token er gyldig (dager).</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
