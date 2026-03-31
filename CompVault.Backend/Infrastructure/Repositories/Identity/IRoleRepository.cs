using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// Repository for roller med ekstra spørringer utover standard CRUD.
/// </summary>
public interface IRoleRepository : IRepository<ApplicationRole>
{
    /// <summary>Finner en rolle basert på navn.</summary>
    Task<ApplicationRole?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Henter alle rollene med tilhørende permissions.</summary>
    Task<IReadOnlyList<ApplicationRole>> GetAllWithPermissionsAsync(CancellationToken cancellationToken = default);

    /// <summary>Sjekker om en rolle har brukere tilknyttet.</summary>
    Task<bool> HasUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle permission-navn for en gitt rolle.</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesForRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle permission-navn for flere rollenavn.</summary>
    Task<IReadOnlyList<string>> GetPermissionNamesForRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);

    /// <summary>Henter permissions basert på navn.</summary>
    Task<IReadOnlyList<Permission>> GetPermissionsByNamesAsync(HashSet<string> names, CancellationToken cancellationToken = default);

    /// <summary>Fjerner alle permissions for en rolle.</summary>
    Task RemoveRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>Legger til permissions for en rolle.</summary>
    Task AddRolePermissionsAsync(IEnumerable<RolePermission> rolePermissions, CancellationToken cancellationToken = default);

    /// <summary>Henter alle tilgjengelige permissions.</summary>
    Task<IReadOnlyList<Permission>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}