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
    /// Henter alle pending dokumenter for en bruker i én spørring.
    /// Inkluderer dokumenter for brukerens avdeling, jobbtittel, og udirigerte dokumenter.
    /// Filtrerer bort dokumenter brukeren allerede har signert og dokumenter som ikke krever signatur.
    /// </summary>
    Task<IReadOnlyList<Document>> GetPendingForUserAsync(
        Guid userId,
        Guid? departmentId,
        string? jobTitle,
        IReadOnlyList<Guid> signedDocumentIds,
        CancellationToken cancellationToken = default);

    /// <summary>Henter dokumenter basert på en liste med IDer.</summary>
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Legger til en versjonsrecord.</summary>
    Task<DocumentVersion> AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task SoftDeleteAsync(Document document, CancellationToken cancellationToken = default);
}