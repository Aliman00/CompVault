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
        Guid documentTypeId, Guid? categoryId, bool includeArchived,
        CancellationToken cancellationToken = default);

    /// <summary>Henter aktive gjeldende dokumenter for en avdeling (inkludert generelle).</summary>
    Task<IReadOnlyList<Document>> GetActiveCurrentForDepartmentAsync(
        Guid departmentId, Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>Henter aktive gjeldende dokumenter for en jobbtittel (inkludert generelle).</summary>
    Task<IReadOnlyList<Document>> GetActiveCurrentForJobTitleAsync(
        string jobTitle, Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>Henter alle aktive gjeldende dokumenter for en type (ingen targeting-filter).</summary>
    Task<IReadOnlyList<Document>> GetAllActiveCurrentAsync(
        Guid documentTypeId, CancellationToken cancellationToken = default);

    /// <summary>Henter dokumenter basert på en liste med IDer.</summary>
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Legger til en versjonsrecord.</summary>
    Task<DocumentVersion> AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task SoftDeleteAsync(Document document, CancellationToken cancellationToken = default);
}