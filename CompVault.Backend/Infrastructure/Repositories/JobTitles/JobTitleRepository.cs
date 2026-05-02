using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.JobTitles;

/// <summary>
/// EF Core-implementasjon av <see cref="IJobTitleRepository"/>.
/// </summary>
public sealed class JobTitleRepository(AppDbContext dbContext) : BaseRepository<JobTitle>(dbContext), IJobTitleRepository
{
    /// <inheritdoc />
    public async Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(jt => jt.Name.ToLower() == name.ToLower(), cancellationToken);

    /// <inheritdoc />
    public Task SoftDeleteAsync(JobTitle jobTitle, CancellationToken cancellationToken = default)
    {
        jobTitle.DeletedAt = DateTime.UtcNow;
        jobTitle.IsActive = false;
        return Task.CompletedTask;
    }
}