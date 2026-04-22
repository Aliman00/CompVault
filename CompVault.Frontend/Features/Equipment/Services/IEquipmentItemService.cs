using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Equipment.Services;

public interface IEquipmentItemService
{
    /// <summary>
    /// Henter alt utstyr fra backend
    /// </summary>
    Task<Result<List<EquipmentItemDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter ett utstyr basert på ID
    /// </summary>
    Task<Result<EquipmentItemDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Henter alt utstyr i en kategori
    /// </summary>
    Task<Result<List<EquipmentItemDto>>> GetByCategoryAsync(Guid categoryId, CancellationToken ct);

    /// <summary>
    /// Oppretter et nytt utstyr
    /// </summary>
    Task<Result<EquipmentItemDto>> CreateAsync(CreateEquipmentItemRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer et eksisterende utstyr
    /// </summary>
    Task<Result<EquipmentItemDto>> UpdateAsync(Guid id, UpdateEquipmentItemRequest request, CancellationToken ct);

    /// <summary>
    /// Sletter et utstyr
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}