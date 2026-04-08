using System.ComponentModel.DataAnnotations;
namespace CompVault.Shared.DTOs.Auth;


public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required")]
    [MinLength(1, ErrorMessage = "RefreshToken cannot be empty")]
    public string RefreshToken { get; set; } = string.Empty;
}