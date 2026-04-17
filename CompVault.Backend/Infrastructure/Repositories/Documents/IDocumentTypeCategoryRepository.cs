using CompVault.Backend.Domain.Entities.Documents;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <summary>
/// Repository for dokumenttypekategorier.
/// </summary>
public interface IDocumentTypeCategoryRepository : IRepository<DocumentTypeCategory>
{
    /// <summary>Henter alle kategorier for en dokumenttype.</summary>
    Task<IReadOnlyList<DocumentTypeCategory>> GetByDocumentTypeIdAsync(
        Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om en slug allerede finnes for en gitt dokumenttype.</summary>
    Task<bool> SlugExistsAsync(Guid documentTypeId, string slug, Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}