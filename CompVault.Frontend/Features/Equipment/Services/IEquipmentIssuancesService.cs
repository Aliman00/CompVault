using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Equipment.Services;

public interface IEquipmentIssuancesService
{
    /// <summary>
    /// Henter alle utleveringer fra backend
    /// </summary>
    Task<Result<List<EquipmentIssuanceDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter én utlevering basert på ID
    /// </summary>
    Task<Result<EquipmentIssuanceDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>
    /// Henter alle utleveringer for en bruker
    /// </summary>
    Task<Result<List<EquipmentIssuanceDto>>> GetByUserAsync(Guid userId, CancellationToken ct);
    
    /// <summary>
    /// Henter alle utleveringer for et utstyr
    /// </summary>
    Task<Result<List<EquipmentIssuanceDto>>> GetByItemAsync(Guid userId, CancellationToken ct);

    /// <summary>
    /// Oppretter en ny utlevering
    /// </summary>
    Task<Result<EquipmentIssuanceDto>> CreateAsync(CreateEquipmentIssuanceRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer en eksisterende utlevering
    /// </summary>
    Task<Result<EquipmentIssuanceDto>> UpdateAsync(Guid id, UpdateEquipmentIssuanceRequest request, 
        CancellationToken ct);

    /// <summary>
    /// Sletter en utlevering
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}