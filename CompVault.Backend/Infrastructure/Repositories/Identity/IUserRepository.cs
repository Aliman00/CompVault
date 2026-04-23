using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// Repository for brukere med ekstra spørringer utover standard CRUD.
/// </summary>
public interface IUserRepository : IRepository<ApplicationUser>
{
    /// <summary>Finner en bruker basert på e-postadressen.</summary>
    Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>Bruker ID til å hente en bruker, med Department og Manager-tabellene</summary>
    Task<ApplicationUser?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Henter alle aktive brukere inkludert rollene deres i én operasjon for å unngå N+1 problemer.</summary>
    Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>> GetActiveUsersWithRolesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>Henter brukere som tilhører en målgruppe. Enten avdeling eller stillingstittel.</summary>
    Task<IReadOnlyList<ApplicationUser>> GetUsersByTargetAsync(IReadOnlyList<Guid> departmentIds,
        IReadOnlyList<Guid> jobTitleIds, CancellationToken ct = default);
    
    /// <summary>Henter alle aktive brukere som ikke er slettet.</summary>
    Task<IReadOnlyList<ApplicationUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter alle direkte underansatte til en gitt leder.</summary>
    Task<IReadOnlyList<ApplicationUser>> GetDirectReportsAsync(Guid managerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Henter alle brukere som har en leder-stillingstittel (IsLeader=true).
    /// Brukes som dropdown-kandidater for brukers nærmeste leder (ManagerId).
    /// </summary>
    Task<IReadOnlyList<ApplicationUser>> GetPotentialManagersAsync(CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter brukeren ved å sette <see cref="ApplicationUser.DeletedAt"/> og <see cref="ApplicationUser.IsActive"/>.</summary>
    Task SoftDeleteAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    /// <summary>Teller aktive brukere som ikke er slettet.</summary>
    Task<int> CountActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter en paginert side med aktive brukere inkludert rollene sine.</summary>
    Task<IReadOnlyList<(ApplicationUser User, List<string> Roles)>> GetActiveUsersWithRolesPagedAsync(
        int skip, int take, CancellationToken cancellationToken = default);
}