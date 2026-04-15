using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;
namespace CompVault.Frontend.Features.Roles.Services;

public interface IRoleService
{
    /// <summary>
    /// Henter alle roller fra backend
    /// </summary>
    Task<Result<List<RoleDto>>> GetAllAsync(CancellationToken ct);
    
    /// <summary>
    /// Henter en rolle fra backend
    /// </summary>
    Task<Result<RoleDto?>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny rolle
    /// </summary>
    Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer eksisterende rolle
    /// </summary>
    Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct);
    
    /// <summary>
    /// Sletter en rolle
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Tilknytter permissions til en rolle (overskriver eksisterende)
    /// </summary>
    Task<Result<RoleDto>> AssignPermissionsAsync(Guid id, AssignPermissionsRequest request, CancellationToken ct);

    /// <summary>
    /// Henter alle tilgjengelige permissions
    /// </summary>
    Task<Result<List<ExpiringCompetencyDto>>> GetAllPermissionsAsync(CancellationToken ct);
}