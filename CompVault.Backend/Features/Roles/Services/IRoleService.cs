using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Roles.Services;

/// <summary>
/// Rolleadministrasjon — henting, oppretting, oppdatering, sletting og permission-tilordning.
/// </summary>
public interface IRoleService
{
    /// <summary>Henter alle roller med tilhørende permissions.</summary>
    Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én rolle med tilhørende permissions.</summary>
    Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny rolle.</summary>
    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer en eksisterende rolle.</summary>
    Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default);

    /// <summary>Sletter en rolle.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Tildeler permissions til en rolle (overskriver eksisterende).</summary>
    Task<Result<RoleDto>> AssignPermissionsAsync(Guid roleId, AssignPermissionsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Henter alle tilgjengelige permissions.</summary>
    Task<Result<IReadOnlyList<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
}