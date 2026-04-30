using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// Repository for utleveringer.
/// </summary>
public interface IEquipmentIssuanceRepository : IRepository<EquipmentIssuance>
{
    /// <summary>Henter alle utleveringer med navigasjonsdata (bruker, utstyr, kategori, utsteder).</summary>
    Task<IReadOnlyList<EquipmentIssuance>> GetAllWithDetailsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returnerer IQueryable med nødvendige Includes for paginering på DB-nivå.
    /// Brukes av service for CountAsync + Skip/Take.
    /// </summary>
    IQueryable<EquipmentIssuance> QueryWithDetails();

    /// <summary>Henter én utlevering med fullstendige navigasjonsdata.</summary>
    Task<EquipmentIssuance?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter én utlevering for oppdatering (tracking query med navigasjon).</summary>
    Task<EquipmentIssuance?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alle utleveringer for en bestemt bruker med navigasjonsdata.</summary>
    Task<IReadOnlyList<EquipmentIssuance>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>Henter utleveringer for en bruker med valgfri kategorifiltrering og paginering. Returner
    /// en liste med utlevert utstyr og totalt antall</summary>
    Task<(IReadOnlyList<EquipmentIssuance> Items, int TotalCount)> GetByUserIdPagedAsync(
        Guid userId, Guid? categoryId, PagedQuery query, CancellationToken ct = default);
    
    /// <summary>Henter alle utleveringer for et bestemt utstyr med navigasjonsdata.</summary>
    Task<IReadOnlyList<EquipmentIssuance>> GetByItemIdAsync(Guid itemId, CancellationToken ct = default);
    
    /// <summary>Henter alle utstyrskategorier der en bruker har fått utlevert utstyur.
    /// Grupperer etter ID og navn, og sorterer etter navn. Teller antall utstyr pr kategori</summary>
    Task<IReadOnlyList<UserEquipmentCategoryDto>> GetCategoriesForUserAsync(Guid userId,
        CancellationToken ct = default);

    /// <summary>Teller utleveringer for en bestemt bruker (for paginering).</summary>
    Task<int> CountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter utleveringen.</summary>
    Task SoftDeleteAsync(EquipmentIssuance issuance, CancellationToken cancellationToken = default);
}
