using CompVault.Backend.Common;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Users;

/// <summary>
/// Alt av brukeradministrasjon — henting, oppretting, oppdatering og sletting.
/// </summary>
public interface IUserService
{
    /// <summary>Henter alle aktive brukere.</summary>
    Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter én bruker basert på ID.</summary>
    Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Oppretter en ny brukerkonto.</summary>
    Task<Result<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Oppdaterer profilfelter på en eksisterende bruker.</summary>
    Task<Result<UserDto>> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter brukeren ved å sette DeletedAt-tidsstempelet.</summary>
    Task<Result<bool>> DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
