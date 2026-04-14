using CompVault.Backend.Domain.Entities.Documents;

namespace CompVault.Backend.Infrastructure.Repositories.Documents;

/// <summary>
/// Repository for dokumenter med spesialiserte spørringer for versjonering og signering.
/// </summary>
public interface IDocumentRepository : IRepository<Document>
{
    /// <summary>Henter et dokument med navigasjon (DocumentType, Category, Uploader).</summary>
    Task<Document?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter et dokument for oppdatering (tracked, ingen AsNoTracking).</summary>
    Task<Document?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter et dokument med signaturer for signering-validering.</summary>
    Task<Document?> GetCurrentWithSignaturesAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alle aktive dokumenter for en gitt dokumenttype.</summary>
    Task<IReadOnlyList<Document>> GetByDocumentTypeAsync(
        Guid documentTypeId, Guid? categoryId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter alle aktive dokumenter rettet mot brukerens avdeling, jobbtittel, eller udirigerte.
    /// Filtrering av signaturkrav og allerede signerte dokumenter gjøres i service-laget.
    /// </summary>
    Task<IReadOnlyList<Document>> GetPendingForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        CancellationToken cancellationToken = default);

    /// <summary>Henter dokumenter basert på en liste med IDer.</summary>
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Legger til en versjonsrecord.</summary>
    Task<DocumentVersion> AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task SoftDeleteAsync(Document document, CancellationToken cancellationToken = default);
}