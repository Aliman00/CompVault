using CompVault.Backend.Domain.Entities.Competencies;

namespace CompVault.Backend.Infrastructure.Repositories.Competencies;

/// <summary>
/// Repository for kompetansetyper med spørring for navn-unikhet og soft delete.
/// </summary>
public interface ICompetencyTypeRepository : IRepository<CompetencyType>
{
    /// <summary>Henter en kompetansetype basert på navn (case-insensitive).</summary>
    Task<CompetencyType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om det finnes aktive kompetansebevis av denne typen.</summary>
    Task<bool> HasActiveCompetenciesAsync(Guid competencyTypeId, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter kompetansetypen ved å sette DeletedAt og IsActive.</summary>
    Task SoftDeleteAsync(CompetencyType competencyType, CancellationToken cancellationToken = default);
}
