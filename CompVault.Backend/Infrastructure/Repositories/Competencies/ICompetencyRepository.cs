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
    /// Teller kompetansebevis med valgfrie filtre (paginering-støtte).
    /// </summary>
    Task<int> CountWithFiltersAsync(
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter paginerte kompetansebevis med navigasjon og valgfrie filtre.
    /// </summary>
    Task<IReadOnlyList<Competency>> GetAllWithDetailsPagedAsync(
        int skip,
        int take,
        Guid? userId,
        CompetencyStatus? status,
        Guid? competencyTypeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter ett kompetansebevis med navigasjon for oppdatering (tracking).
    /// Brukes av CompetencyService.UpdateAsync for å unngå ekstra queries.
    /// </summary>
    Task<Competency?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Oppdaterer status på kompetansebevis basert på utløpsdato via ren SQL.
    /// Returnerer antall oppdaterte for Expired og ExpiringSoon,
    /// pluss liste over berørte kompetansebevis med ID og gammel/ny status.
    /// Berører aldri Revoked-bevis eller soft-deleted rader (global query filter).
    /// </summary>
    Task<(int ExpiredCount, int ExpiringSoonCount, List<(Guid CompetencyId, CompetencyStatus OldStatus, CompetencyStatus NewStatus)> StatusChanges)> UpdateExpiryStatusesAsync(CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter kompetansebeviset ved å sette DeletedAt og IsActive.</summary>
    Task SoftDeleteAsync(Competency competency, CancellationToken cancellationToken = default);
}