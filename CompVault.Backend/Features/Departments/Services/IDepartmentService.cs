using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Departments.Services;

/// <summary>
/// Avdelingsadministrasjon — henting, oppretting, oppdatering og sletting.
/// </summary>
public interface IDepartmentService
{
    /// <summary>Henter alle aktive avdelinger.</summary>
    Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én avdeling basert på ID.</summary>
    Task<Result<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny avdeling.</summary>
    Task<Result<DepartmentDto>> CreateAsync(Guid userId, CreateDepartmentRequest request, CancellationToken ct = default);

    /// <summary>Oppdaterer en eksisterende avdeling.</summary>
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter en avdeling.</summary>
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}