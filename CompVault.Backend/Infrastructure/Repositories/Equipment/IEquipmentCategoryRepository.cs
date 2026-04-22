using CompVault.Backend.Domain.Entities.Equipment;

namespace CompVault.Backend.Infrastructure.Repositories.Equipment;

/// <summary>
/// Repository for utstyrskategorier.
/// </summary>
public interface IEquipmentCategoryRepository : IRepository<EquipmentCategory>
{
    /// <summary>Henter alle kategorier med tilhørende utstyr.</summary>
    Task<IReadOnlyList<EquipmentCategory>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én kategori med tilhørende utstyr (no-tracking).</summary>
    Task<EquipmentCategory?> GetByIdWithItemsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter én kategori med tilhørende utstyr for mutasjon (tracking).</summary>
    Task<EquipmentCategory?> GetByIdWithItemsForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter kategorien.</summary>
    Task SoftDeleteAsync(EquipmentCategory category, CancellationToken cancellationToken = default);
}