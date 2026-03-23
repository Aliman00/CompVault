namespace CompVault.Shared.DTOs.Auth;

public class RefreshTokenResponse
{
    /// <summary>Det signerte JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Refresh token for å hente et nytt access token når det utgår.</summary>
    public string RefreshToken { get; set; } = string.Empty;
}