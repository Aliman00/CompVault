using CompVault.Backend.Domain.Entities.Equipment;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// Repository for utstyr.
/// </summary>
public interface IEquipmentItemRepository : IRepository<EquipmentItem>
{
    /// <summary>Henter alt utstyr med kategori-information.</summary>
    Task<IReadOnlyList<EquipmentItem>> GetAllWithCategoryAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter ett utstyr med kategori (no-tracking).</summary>
    Task<EquipmentItem?> GetByIdWithCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter ett utstyr med kategori for mutasjon (tracking).</summary>
    Task<EquipmentItem?> GetByIdTrackedAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alt utstyr i en bestemt kategori.</summary>
    Task<IReadOnlyList<EquipmentItem>> GetByCategoryIdAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om utstyret har aktive utleveringer.</summary>
    Task<bool> HasActiveIssuancesAsync(Guid itemId, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter utstyret.</summary>
    Task SoftDeleteAsync(EquipmentItem item, CancellationToken cancellationToken = default);
}