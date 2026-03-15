namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Svaret klienten får etter vellykket innlogging eller token-refresh.
/// </summary>
public sealed class LoginResponse
{
    /// <summary>Det signerte JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh token for å hente et nytt access token når det utgår.</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>ID til innlogget bruker.</summary>
    public Guid UserId { get; set; }

    /// <summary>Fullt navn på innlogget bruker.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Rollene brukeren har.</summary>
    public IReadOnlyList<string> Roles { get; set; } = [];
}
