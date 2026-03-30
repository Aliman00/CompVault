namespace CompVault.Shared.DTOs.Auth;

public class TokenResponse
{
    /// <summary>Det signerte JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Token som lagres hos klienten for å holde brukeren innlogget når access går ut</summary>
    public string RefreshToken { get; set; } = string.Empty;
}