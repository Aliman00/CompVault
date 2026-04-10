using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Users.Services;

public interface IUserService
{
    /// <summary>
    /// Henter alle aktive brukere fra backend
    /// </summary>
    /// <returns>En liste med UserDto</returns>
    Task<Result<List<UserDto>>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Henter en aktiv bruker fra backend
    /// </summary>
    /// <returns>UserDto eller null</returns>
    Task<Result<UserDto?>> GetByIdAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Oppdaterer eksisterende bruker
    /// </summary>
    /// <param name="id">Brukerens ID</param>
    /// <param name="request">UpdateUserRequest</param>
    /// <param name="ct"></param>
    /// <returns>Result med Success eller Failure</returns>
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);

}