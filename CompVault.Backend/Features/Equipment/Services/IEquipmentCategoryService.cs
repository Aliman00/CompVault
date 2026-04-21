using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Service for administrasjon av utstyrskategorier.
/// </summary>
public interface IEquipmentCategoryService
{
    Task<Result<IReadOnlyList<EquipmentCategoryDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<EquipmentCategoryDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<EquipmentCategoryDto>> CreateAsync(CreateEquipmentCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<EquipmentCategoryDto>> UpdateAsync(Guid id, UpdateEquipmentCategoryRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}