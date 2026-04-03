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
}