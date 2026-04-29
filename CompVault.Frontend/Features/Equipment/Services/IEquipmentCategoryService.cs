using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Equipment.Services;

public interface IEquipmentCategoryService
{
    /// <summary>
    /// Henter alle utstyrskategorier fra backend
    /// </summary>
    Task<Result<List<EquipmentCategoryDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter en utstyrskategori basert på ID
    /// </summary>
    Task<Result<EquipmentCategoryDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny utstyrskategori
    /// </summary>
    Task<Result<EquipmentCategoryDto>> CreateAsync(CreateEquipmentCategoryRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer en eksisterende utstyrskategori
    /// </summary>
    Task<Result<EquipmentCategoryDto>> UpdateAsync(Guid id, UpdateEquipmentCategoryRequest request, 
        CancellationToken ct);

    /// <summary>
    /// Sletter en utstyrskategori
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}