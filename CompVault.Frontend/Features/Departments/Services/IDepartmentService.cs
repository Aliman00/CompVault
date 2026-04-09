using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Departments.Services;

public interface IDepartmentService
{
    /// <summary>
    /// Henter alle departments fra backend
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>Liste med DepartmentDto</returns>
    Task<Result<List<DepartmentDto>>> GetAllAsync(CancellationToken ct);
}