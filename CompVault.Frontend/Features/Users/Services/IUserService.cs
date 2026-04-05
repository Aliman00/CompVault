using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Users.Services;

public interface IUserService
{
    /// <summary>
    /// Henter alle aktive brukere fra backend
    /// </summary>
    /// <returns>En liste med UserDto</returns>
    Task<Result<List<UserDto>>> GetAllUsersAsync(CancellationToken ct);

    /// <summary>
    /// Henter en aktiv bruker fra backend
    /// </summary>
    /// <returns>UserDto eller null</returns>
    Task<Result<UserDto?>> GetByIdAsync(Guid id, CancellationToken ct);

}