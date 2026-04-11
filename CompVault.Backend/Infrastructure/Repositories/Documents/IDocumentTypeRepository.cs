using CompVault.Backend.Domain.Entities.Documents;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <summary>
/// Repository for dokumenttyper.
/// </summary>
public interface IDocumentTypeRepository : IRepository<DocumentType>
{
    /// <summary>Henter en dokumenttype basert på slug.</summary>
    Task<DocumentType?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Henter en dokumenttype med tilhørende kategorier.</summary>
    Task<DocumentType?> GetWithCategoriesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter en dokumenttype med tilhørende kategorier basert på slug.</summary>
    Task<DocumentType?> GetWithCategoriesBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om en slug allerede er i bruk.</summary>
    Task<bool> SlugExistsAsync(string slug, Guid? excludeId = null, CancellationToken cancellationToken = default);
}