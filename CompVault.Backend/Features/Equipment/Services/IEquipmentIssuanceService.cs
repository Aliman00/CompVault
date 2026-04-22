using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Service for administrasjon av utleveringer.
/// </summary>
public interface IEquipmentIssuanceService
{
    Task<Result<IReadOnlyList<EquipmentIssuanceDto>>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Result<EquipmentIssuanceDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<EquipmentIssuanceDto>>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Result<EquipmentIssuanceDto>> CreateAsync(Guid issuedById, CreateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default);
    Task<Result<EquipmentIssuanceDto>> UpdateAsync(Guid id, UpdateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}