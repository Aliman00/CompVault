using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// Repository for brukere med ekstra spørringer utover standard CRUD.
/// </summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    /// <summary>Finner en bruker basert på e-postadressen.</summary>
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Henter alle aktive brukere inkludert rollene deres i én operasjon for å unngå N+1 problemer.</summary>
    Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>> GetActiveUsersWithRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter alle aktive brukere som ikke er slettet.</summary>
    Task<IReadOnlyList<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter alle direkte underansatte til en gitt leder.</summary>
    Task<IReadOnlyList<ApplicationUser>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter brukeren ved å sette <see cref="ApplicationUser.DeletedAt"/> og <see cref="ApplicationUser.IsActive"/>.</summary>
    Task SoftDeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}
