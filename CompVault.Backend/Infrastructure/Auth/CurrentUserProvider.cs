using System.Security.Claims;

using CompVault.Backend.Infrastructure.Extensions;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Implementasjon av <see cref="ICurrentUserProvider"/> som henter informasjon fra den nåværende HTTP-konteksten.
/// </summary>
public sealed class CurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    /// <inheritdoc />
    public Guid? GetCurrentUserId()
    {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
            return null;

        try
        {
            return user.GetUserId();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}