namespace CompVault.Shared.DTOs.Auth;

public class RefreshTokenResponse
{
    /// <summary>Det signerte JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// Refresh token returnert i body kun for at frontend kan sette HttpOnly cookie
    /// Skal aldri eksponeres mot nettleseren direkte.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}