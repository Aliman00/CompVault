using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Departments.Services;

public interface IDepartmentService
{
    /// <summary>
    /// Henter alle departments fra backend
    /// </summary>
    Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter en avdeling fra backend
    /// </summary>
    Task<Result<DepartmentDto?>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny avdeling
    /// </summary>
    Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer eksisterende avdeling
    /// </summary>
    Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken ct);

    /// <summary>
    /// Sletter en avdeling
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}