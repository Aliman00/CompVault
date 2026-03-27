using CompVault.Backend.Domain.Entities.Competencies;
using CompVault.Shared.Enums;

namespace CompVault.Backend.Infrastructure.Repositories.Competencies;

/// <summary>
/// Repository for kompetansebevis med navigasjonsinkludering, filtrering og batch-oppdatering.
/// </summary>
public interface ICompetencyRepository : IRepository<Competency>
{
    /// <summary>Henter ett kompetansebevis med ApplicationUser og CompetencyType navigasjon.</summary>
    Task<Competency?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter kompetansebevis med navigasjon, filtrert på valgfrie parametere.
    /// Alle parametere er nullable — null betyr ingen filtrering.
    /// </summary>
    Task<IReadOnlyList<Competency>> GetAllWithDetailsAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter kompetansebevis med EXPIRING_SOON eller EXPIRED status,
    /// filtrert på valgfrie userId og departmentId.
    /// Brukes av /api/competencies/expiring-endpointen.
    /// </summary>
    Task<IReadOnlyList<Competency>> GetExpiringAsync(
        Guid? userId,
        Guid? departmentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter alle kompetansebevis hvor Status != Revoked, for bakgrunnsjobben.
    /// </summary>
    Task<IReadOnlyList<Competency>> GetAllForStatusUpdateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Batch-oppdaterer status på flere kompetansebevis effektivt.
    /// </summary>
    Task UpdateStatusesAsync(IEnumerable<(Guid Id, CompetencyStatus NewStatus)> updates, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter kompetansebeviset ved å sette DeletedAt og IsActive.</summary>
    Task SoftDeleteAsync(Competency competency, CancellationToken cancellationToken = default);
}
