namespace CompVault.Backend.Features.Auth.DTOs;

/// <summary>
/// Intern DTO for å sende Access- og Refresh-token mellom lagene
/// </summary>
public class TokenDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}