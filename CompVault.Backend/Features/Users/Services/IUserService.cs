using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Users.Services;

/// <summary>
/// Alt av brukeradministrasjon — henting, oppretting, oppdatering og sletting.
/// </summary>
public interface IUserService
{
    /// <summary>Henter paginerte aktive brukere.</summary>
    Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(PagedQuery query, CancellationToken cancellationToken = default);

    /// <summary>Henter én bruker basert på ID.</summary>
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    
    /// <summary>Henter alle brukere innlogget bruker har lov til å hente</summary>
    Task<Result<IReadOnlyList<UserLookupDto>>> LookupAllowedUsersAsync(string bypassPermission = Permissions.UsersAll,
        string subPermission = Permissions.UsersReadSub, CancellationToken ct = default);

    /// <summary>Oppretter en ny brukerkonto.</summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer profilfelter på en eksisterende bruker.</summary>
    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Henter alle brukere som har en leder-stillingstittel (IsLeader=true).
    /// Brukes som dropdown-kandidater for brukers nærmeste leder (ManagerId).
    /// </summary>
    Task<Result<IReadOnlyList<UserDto>>> GetPotentialManagersAsync(CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter brukeren ved å sette DeletedAt-tidsstempelet.</summary>
    Task<Result<bool>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}