using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Shared.DTOs.Documents;
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

    /// <summary>Henter alle dokumenttyper hvor brukeren har dokumenter.
    /// Henter dokumentene og grupperer de, deretter henter ut det vi trenger for DTO-en,
    /// sorteret etter navn</summary>
    Task<IReadOnlyList<UserDocumentTypeDto>> GetDocumentTypesForUserAsync(Guid userId, Guid? departmentId,
        Guid? jobTitleId, CancellationToken ct = default);

    /// <summary>Teller dokumenter for en bruker basert på filtrering av status</summary>
    Task<int> CountDocumentsForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        DocumentQueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>Henter alle dokumentene for en bruker med paginering og fitlering.
    /// Sjekker at brukeren har tilattelse til å hente ut dokumetnene ved at det er enten brukeren
    /// selv som henter eller at brukeren er like høyt eller høyere i hierarkiet</summary>
    Task<IReadOnlyList<Document>> GetDocumentsForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        DocumentQueryParameters parameters,
        CancellationToken ct = default);

    /// <summary>Legger til en versjonsrecord.</summary>
    Task<DocumentVersion> AddVersionAsync(DocumentVersion version, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter et dokument.</summary>
    Task SoftDeleteAsync(Document document, CancellationToken cancellationToken = default);
}