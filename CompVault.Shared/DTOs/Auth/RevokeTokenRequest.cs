using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Request for å ugyldiggjøre (revoke) et refresh token.
/// </summary>
public record RevokeTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; init; } = string.Empty;

    public RevokeTokenRequest() { }

    public RevokeTokenRequest(string refreshToken)
    {
        RefreshToken = refreshToken;
    }
}
