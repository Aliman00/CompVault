using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Departments;
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
    IDepartmentRepository departmentRepository,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ILogger<UserService> logger) : IUserService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserDto>>> GetAllUsersAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<(ApplicationUser User, List<string> Roles)> usersWithRoles = 
            await userRepository.GetActiveUsersWithRolesAsync(cancellationToken);

        var dtos = usersWithRoles
            .Select(uwr => UserMapper.ToDto(uwr.User, uwr.Roles))
            .ToList();

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> GetUserByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdWithDetailsAsync(userId, cancellationToken);

        if (user is null || user.DeletedAt is not null || !user.IsActive)
            return Result<UserDto>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        IList<string> roles = await userManager.GetRolesAsync(user);
        return Result<UserDto>.Success(UserMapper.ToDto(user, roles));
    }

    /// <inheritdoc />
    public async Task<Result<UserDto>> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        bool emailTaken = await userRepository.ExistsAsync(
            u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

        if (emailTaken)
        {
            logger.LogWarning("Kunne ikke opprette bruker: e-post {Email} er allerede i bruk", request.Email);
            return Result<UserDto>.Failure(
                AppError.Conflict($"En bruker med e-post '{request.Email}' eksisterer allerede."));
        }

        // Valider at avdelingen eksisterer hvis DepartmentId er angitt
        if (request.DepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.DepartmentId.Value && d.IsActive && d.DeletedAt == null, cancellationToken);

            if (!departmentExists)
            {
                logger.LogWarning("Kunne ikke opprette bruker: avdeling {DepartmentId} ble ikke funnet", request.DepartmentId.Value);
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.DepartmentId.Value}' ble ikke funnet."));
            }
        }

        // Valider at lederen eksisterer og er aktiv hvis ManagerId er angitt
        if (request.ManagerId.HasValue)
        {
            bool managerExists = await userRepository.ExistsAsync(
                u => u.Id == request.ManagerId.Value && u.IsActive && u.DeletedAt == null, cancellationToken);

            if (!managerExists)
            {
                logger.LogWarning("Kunne ikke opprette bruker: leder {ManagerId} ble ikke funnet eller er inaktiv", request.ManagerId.Value);
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet eller er inaktiv."));
            }
        }

        // Valider roller FØR bruker opprettes for å unngå foreldreløse brukere
        List<string> validRoles = new();
        if (request.Roles.Count > 0)
        {
            foreach (string roleName in request.Roles)
            {
                bool exists = await roleManager.RoleExistsAsync(roleName);
                if (!exists)
                {
                    logger.LogWarning("Kunne ikke opprette bruker: rolle {Role} eksisterer ikke", roleName);
                    return Result<UserDto>.Failure(
                        AppError.Create(ErrorCode.Validation, $"Rollen '{roleName}' eksisterer ikke."));
                }
                validRoles.Add(roleName);
            }
        }

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
            logger.LogWarning("Kunne ikke opprette bruker {Email}: {Errors}", request.Email, errorMessage);
            return Result<UserDto>.Failure(
                AppError.Create(ErrorCode.Validation, errorMessage));
        }

        if (validRoles.Count > 0)
        {
            IdentityResult roleResult = await userManager.AddToRolesAsync(newUser, validRoles);
            if (!roleResult.Succeeded)
            {
                string roleErrors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
                logger.LogWarning("Rolletilordning feilet for {Email}: {Errors}", request.Email, roleErrors);
                return Result<UserDto>.Failure(
                    AppError.Create(ErrorCode.Validation, roleErrors));
            }
        }

        logger.LogInformation("Bruker {Email} opprettet", request.Email);
        IList<string> roles = await userManager.GetRolesAsync(newUser);
        return Result<UserDto>.Success(UserMapper.ToDto(newUser, roles));
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
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        if (request.ManagerId.HasValue && request.ManagerId.Value == userId)
            return Result<UserDto>.Failure(
                AppError.Create(ErrorCode.Validation, "En bruker kan ikke være sin egen leder."));

        if (request.DepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.DepartmentId.Value && d.IsActive && d.DeletedAt == null, cancellationToken);
            if (!departmentExists)
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.DepartmentId.Value}' ble ikke funnet."));
        }

        if (request.ManagerId.HasValue)
        {
            bool managerExists = await userRepository.ExistsAsync(
                u => u.Id == request.ManagerId.Value && u.IsActive && u.DeletedAt == null, cancellationToken);
            if (!managerExists)
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet eller er inaktiv."));
        }

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.JobTitle is not null) user.JobTitle = request.JobTitle;
        if (request.EmploymentType.HasValue) user.EmploymentType = request.EmploymentType.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        if (request.DepartmentId.HasValue)
            user.DepartmentId = request.DepartmentId;
        else if (request.ClearDepartmentId)
            user.DepartmentId = null;

        if (request.ManagerId.HasValue)
            user.ManagerId = request.ManagerId;
        else if (request.ClearManagerId)
            user.ManagerId = null;

        await userRepository.UpdateAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Bruker {UserId} oppdatert", userId);
        IList<string> roles = await userManager.GetRolesAsync(user);
        return Result<UserDto>.Success(UserMapper.ToDto(user, roles));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        ApplicationUser? user = await userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.DeletedAt is not null || !user.IsActive)
            return Result<bool>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        await userRepository.SoftDeleteAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Bruker {UserId} slettet (soft delete)", userId);
        return Result<bool>.Success(true);
    }
}