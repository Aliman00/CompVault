namespace CompVault.Backend.Dev;

/// <summary>
/// Forespørsel for utvikler-innlogging med e-post og passord.
/// ADVARSEL: Kun for Development-miljøet — fjernes sammen med Dev/-mappen før produksjon.
/// </summary>
public sealed class DevLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
