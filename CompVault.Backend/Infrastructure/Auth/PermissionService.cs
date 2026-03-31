using CompVault.Backend.Infrastructure.Repositories.Identity;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Implementerer <see cref="IPermissionService"/>.
/// </summary>
public sealed class PermissionService(IRoleRepository roleRepository) : IPermissionService
{
    /// <inheritdoc />
    public async Task<List<string>> GetPermissionsForRolesAsync(
        IEnumerable<string> roleNames,
        CancellationToken ct)
    {
        if (roleNames is null)
            return [];

        IReadOnlyList<string> permissions = await roleRepository.GetPermissionNamesForRolesAsync(roleNames, ct);
        return permissions.ToList();
    }
}
