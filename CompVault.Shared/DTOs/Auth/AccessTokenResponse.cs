namespace CompVault.Shared.DTOs.Auth;

public class AccessTokenResponse
{
    /// <summary>Det signerte JWT access token.</summary>
    public string AccessToken { get; set; } = string.Empty;
}