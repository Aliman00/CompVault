using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Auth;

/// <summary>
/// Det klienten sender inn for å bytte ut et utgått access token.
/// </summary>
public sealed class RefreshTokenRequest
{
    /// <summary>Refresh token brukt til å verifisere at klienten fortsatt er gyldig.</summary>
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
