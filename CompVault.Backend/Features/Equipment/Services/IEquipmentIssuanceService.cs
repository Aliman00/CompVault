using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Service for administrasjon av utleveringer.
/// </summary>
public interface IEquipmentIssuanceService
{
    Task<Result<PagedResult<EquipmentIssuanceDto>>> GetAllAsync(PagedQuery query, CancellationToken cancellationToken = default);
    Task<Result<EquipmentIssuanceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<EquipmentIssuanceDto>>> GetByUserAsync(Guid userId, PagedQuery query, CancellationToken cancellationToken = default);

    /// <summary> Henter alle utleveringer for et utstyr </summary>
    Task<Result<IReadOnlyList<EquipmentIssuanceDto>>> GetByItemAsync(Guid equipmentItemId,
        CancellationToken ct = default);

    /// <summary>Henter alle utstyrskategorier for innlogget bruker.</summary>
    Task<Result<IReadOnlyList<UserEquipmentCategoryDto>>> GetCategoriesForUserAsync(Guid userId,
        CancellationToken ct = default);

    /// <summary>Henter utleveringer for innlogget bruker med valgfri kategorifiltrering.</summary>
    Task<Result<PagedResult<EquipmentIssuanceDto>>> GetMyEquipmentAsync(Guid userId, Guid? categoryId,
        PagedQuery query, CancellationToken ct = default);

    Task<Result<EquipmentIssuanceDto>> CreateAsync(Guid issuedById, CreateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default);
    Task<Result<EquipmentIssuanceDto>> UpdateAsync(Guid id, UpdateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}