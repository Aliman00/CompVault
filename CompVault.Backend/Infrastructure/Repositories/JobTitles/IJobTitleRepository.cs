using CompVault.Backend.Domain.Entities.JobTitles;

namespace CompVault.Backend.Infrastructure.Repositories.JobTitles;

/// <summary>
/// Repository for stillingstitler med spørringer for navneunikhet og soft-sletting.
/// </summary>
public interface IJobTitleRepository : IRepository<JobTitle>
{
    /// <summary>Henter én stillingstittel basert på navn.</summary>
    Task<JobTitle?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om en stillingstittel med angitt navn allerede finnes.</summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter stillingstittelen ved å sette DeletedAt og IsActive.</summary>
    Task SoftDeleteAsync(JobTitle jobTitle, CancellationToken cancellationToken = default);
}