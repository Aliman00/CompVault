using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;

namespace CompVault.Frontend.Features.Users.Services;

public interface IUserService
{
    /// <summary>
    /// Henter alle aktive brukere fra backend
    /// </summary>
    Task<Result<PagedResult<UserDto>>> GetAllAsync(PagedQuery query, CancellationToken ct);

    /// <summary>
    /// Henter en aktiv bruker fra backend
    /// </summary>
    Task<Result<UserDto?>> GetByIdAsync(Guid id, CancellationToken ct);
    
    /// <summary>
    /// Henter alle brukere med leder-stillingstittel (IsLeader=true)
    /// </summary>
    Task<Result<IReadOnlyList<UserDto>>> GetPotentialManagersAsync(CancellationToken ct);

    /// <summary>
    /// Oppretter en ny bruker
    /// </summary>
    Task<Result<UserDto>> CreateAsync(CreateUserRequest request, CancellationToken ct);

    /// <summary>
    /// Oppdaterer eksisterende bruker
    /// </summary>
    Task<Result<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken ct);

    /// <summary>
    /// Soft-deleter en bruker
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
}