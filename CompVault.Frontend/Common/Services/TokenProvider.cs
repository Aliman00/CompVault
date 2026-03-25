namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Lagrer access--token for den aktive brukeren pr krets
/// Scoped for å ikke kunne deles mellom faner
/// </summary>
public class TokenProvider
{
    public string? AccessToken { get; set; }
}