using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;

namespace CompVault.Frontend.Common.Services;

/// <summary>
/// Henter autentiseringstilstanden fra HttpContexen som bestemmes om bruker har auth-cookie
/// </summary>
public class AuthStateProvider(IHttpContextAccessor httpContextAccessor) : AuthenticationStateProvider
{
    /// <inheritdoc />
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        ClaimsPrincipal user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

        return Task.FromResult(new AuthenticationState(user));
    }
}