using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Result;
using Microsoft.AspNetCore.Identity;

namespace CompVault.Backend.Features.Users.Services;

/// <summary>
/// Implementerer brukeradministrasjon ved hjelp av repository, Identity og Unit of Work.
/// </summary>
public sealed class UserService(
    IUserRepository userRepository,
    UserManager<ApplicationUser> userManager,
    IUnitOfWork unitOfWork) : IUserService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationUser> users = await userRepository.GetActiveUsersAsync(cancellationToken);
        List<UserDto> dtos = new(users.Count);

        foreach (ApplicationUser user in users)
        {
            IList<string> roles = await userManager.GetRolesAsync(user);
            dtos.Add(MapToDto(user, roles));
        }

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.DeletedAt is not null || !user.IsActive)
            return Result<UserDto>.Failure(
                AppError.NotFound($"User with ID '{userId}' was not found."));

        IList<string> roles = await userManager.GetRolesAsync(user);
        return Result<UserDto>.Success(MapToDto(user, roles));
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        bool emailTaken = await userRepository.ExistsAsync(
            u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailTaken)
            return Result<UserDto>.Failure(
                AppError.Conflict($"A user with email '{request.Email}' already exists."));

        ApplicationUser newUser = new()
        {
            UserName = request.Email.ToLowerInvariant(),
            Email = request.Email.ToLowerInvariant(),
            FirstName = request.FirstName,
            LastName = request.LastName,
            JobTitle = request.JobTitle,
            EmploymentType = request.EmploymentType,
            DepartmentId = request.DepartmentId,
            ManagerId = request.ManagerId,
            CreatedAt = DateTime.UtcNow
        };

        IdentityResult createResult = await userManager.CreateAsync(newUser);
        if (!createResult.Succeeded)
        {
            string errorMessage = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result<UserDto>.Failure(
                AppError.Create(ErrorCode.Validation, errorMessage));
        }

        if (request.Roles.Count > 0)
        {
            await userManager.AddToRolesAsync(newUser, request.Roles);
        }

        IList<string> roles = await userManager.GetRolesAsync(newUser);
        return Result<UserDto>.Success(MapToDto(newUser, roles));
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.DeletedAt is not null || (!user.IsActive && request.IsActive != true))
            return Result<UserDto>.Failure(
                AppError.NotFound($"User with ID '{userId}' was not found."));

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.JobTitle is not null) user.JobTitle = request.JobTitle;
        if (request.EmploymentType.HasValue) user.EmploymentType = request.EmploymentType.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;
        if (request.DepartmentId.HasValue) user.DepartmentId = request.DepartmentId;
        if (request.ManagerId.HasValue) user.ManagerId = request.ManagerId;

        await userRepository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        IList<string> roles = await userManager.GetRolesAsync(user);
        return Result<UserDto>.Success(MapToDto(user, roles));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.DeletedAt is not null || !user.IsActive)
            return Result<bool>.Failure(
                AppError.NotFound($"User with ID '{userId}' was not found."));

        await userRepository.SoftDeleteAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result<bool>.Success(true);
    }

    private static UserDto MapToDto(ApplicationUser user, IList<string> roles) =>
        new()
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            JobTitle = user.JobTitle,
            EmploymentType = user.EmploymentType,
            IsActive = user.IsActive,
            DepartmentId = user.DepartmentId,
            ManagerId = user.ManagerId,
            CreatedAt = user.CreatedAt,
            Roles = roles.ToList()
        };
}
