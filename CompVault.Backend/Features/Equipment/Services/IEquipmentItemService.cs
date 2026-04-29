using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Service for administrasjon av utstyr.
/// </summary>
public interface IEquipmentItemService
{
    Task<Result<IReadOnlyList<EquipmentItemDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<EquipmentItemDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EquipmentItemDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<Result<EquipmentItemDto>> CreateAsync(CreateEquipmentItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<EquipmentItemDto>> UpdateAsync(Guid id, UpdateEquipmentItemRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}