using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;

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

    /// <summary>
    /// Henter alle aktive dokumenter for en dokumenttype som er synlige for brukeren
    /// (basert på avdeling/jobbtittel-targeting).
    /// </summary>
    Task<IReadOnlyList<Document>> GetAccessibleByDocumentTypeAsync(
        Guid documentTypeId,
        Guid? departmentId,
        Guid? jobTitleId,
        CancellationToken cancellationToken = default);

    /// <summary>Henter dokumenter basert på en liste med IDer.</summary>
    Task<IReadOnlyList<Document>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    
    /// <summary>Teller dokumenter for en bruker basert på filtrering av status</summary>
    Task<int> CountDocumentsForUserAsync(
        Guid userId,
        Guid? departmentId,
        Guid? jobTitleId,
        DocumentSignatureFilter signatureFilter,
        CancellationToken ct = default);

    /// <summary>Henter alle dokumentene for en bruker med paginering og fitlering.
    /// Sjekker at brukeren har tilattelse til å hente ut dokumetnene ved at det er enten brukeren
    /// selv som henter eller at brukeren er like høyt eller høyere i hierarkiet</summary>
    Task<IReadOnlyList<Document>> GetDocumentsForUserPagedAsync(
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