namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Slår opp permissions for en gitt liste med roller.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Henter alle permissions som er tilordnet de gitte rollene.
    /// </summary>
    Task<List<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames, CancellationToken ct);
}
