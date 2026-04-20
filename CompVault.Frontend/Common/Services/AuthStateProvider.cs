using Microsoft.AspNetCore.Components.Authorization;
namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Henter autentiseringstilstanden fra CircuitUserContext som henter brukeren fra HttpContext under SSR-fasen,
/// og holder den tilgjengelig gjennom SignalR-kretsen der HttpContext er null
/// </summary>
public class AuthStateProvider(CircuitUserContext circuitUserContext) : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
        => Task.FromResult(new AuthenticationState(circuitUserContext.User));

    /// <summary>
    /// Re-renderer komponenter med oppdatert AuthenticationState 
    /// </summary>
    public void NotifyStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}