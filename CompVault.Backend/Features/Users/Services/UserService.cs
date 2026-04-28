using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Shared.Constants;
using CompVault.Shared.DTOs.Common.Pagination;
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
    IJobTitleRepository jobTitleRepository,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IDepartmentScopeService departmentScope,
    ILogger<UserService> logger,
    IUnitOfWork unitOfWork) : IUserService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<UserDto>>> GetAllUsersAsync(
        PagedQuery query, CancellationToken cancellationToken = default)
    {
        int totalCount = await userRepository.CountActiveAsync(cancellationToken);
        IReadOnlyList<(ApplicationUser User, List<string> Roles)> usersWithRoles =
            await userRepository.GetActiveUsersWithRolesPagedAsync(query.Skip, query.PageSize, cancellationToken);

        var dtos = usersWithRoles
            .Select(uwr => UserMapper.ToDto(uwr.User, uwr.Roles))
            .ToList();

        return Result<PagedResult<UserDto>>.Success(
            PagedResult<UserDto>.Create(dtos, totalCount, query));
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
    public async Task<Result<IReadOnlyList<UserLookupDto>>> LookupAllowedUsersAsync(
        string bypassPermission = Permissions.UsersAll,
        string subPermission = Permissions.UsersReadSub,
        CancellationToken ct = default)
    {
        // Sjekker om brukeren kan hente brukere i underavdelinger, kun fra sin egen eller alle brukere
        bool bypass = departmentScope.HasBypass(bypassPermission);
        IReadOnlyList<Guid> allowedIds = departmentScope.GetAllowedDepartmentIds(subPermission);

        IReadOnlyList<ApplicationUser> users = await userRepository.GetLookupAsync(allowedIds, bypass, ct);
        return Result<IReadOnlyList<UserLookupDto>>.Success(users.Select(u => u.ToLookupDto()).ToList());
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
        bool departmentExists = await departmentRepository.ExistsAsync(
            d => d.Id == request.DepartmentId && d.IsActive && d.DeletedAt == null, cancellationToken);
        if (!departmentExists)
        {
            logger.LogWarning("Kunde ikke opprette bruker: avdeling {DepartmentId} ble ikke funnet", 
                request.DepartmentId);
            return Result<UserDto>.Failure(
                AppError.NotFound($"Avdeling med ID '{request.DepartmentId}' ble ikke funnet."));
        }

        // Valider at lederen eksisterer og er aktiv hvis ManagerId er angitt
        if (request.ManagerId.HasValue)
        {
            ApplicationUser? manager = await userRepository.GetByIdIgnoringFiltersAsync(
                request.ManagerId.Value, cancellationToken);

            if (manager is null || !manager.IsActive)
            {
                logger.LogWarning("Kunne ikke opprette bruker: leder {ManagerId} ble ikke funnet eller er inaktiv", 
                    request.ManagerId.Value);
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet eller er inaktiv."));
            }

            if (!departmentScope.IsAllowed(manager.DepartmentId, Permissions.UsersAll, Permissions.UsersReadSub))
                return Result<UserDto>.Failure(
                    AppError.Create(ErrorCode.Forbidden, 
                        "Du har ikke tilgang til å sette denne brukeren som leder."));
        }

        // Valider at stillingstittelen eksisterer hvis JobTitleId er angitt
        if (request.JobTitleId.HasValue)
        {
            bool jobTitleExists = await jobTitleRepository.ExistsAsync(
                jt => jt.Id == request.JobTitleId.Value && jt.IsActive, cancellationToken);

            if (!jobTitleExists)
            {
                logger.LogWarning("Kunne ikke opprette bruker: stillingstittel {JobTitleId} ble ikke funnet", request.JobTitleId.Value);
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Stillingstittel med ID '{request.JobTitleId.Value}' ble ikke funnet."));
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
            JobTitleId = request.JobTitleId,
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
        CancellationToken ct = default)
    {
        ApplicationUser? user = await userRepository.GetByIdWithDetailsAsync(userId, ct);

        if (user is null || user.DeletedAt is not null || (!user.IsActive && request.IsActive != true))
            return Result<UserDto>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            bool emailExist = await userRepository.ExistsAsync(
                u => u.Email == request.Email && u.Id != user.Id, ct);
            if (emailExist)
                return Result<UserDto>.Failure(
                    AppError.Create(ErrorCode.Conflict, "E-posten er allerede i bruk."));
        }

        if (request.ManagerId.HasValue && request.ManagerId.Value == userId)
            return Result<UserDto>.Failure(
                AppError.Create(ErrorCode.Validation, "En bruker kan ikke være sin egen leder."));

        if (request.DepartmentId.HasValue)
        {
            bool departmentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.DepartmentId.Value && d.IsActive && d.DeletedAt == null, ct);
            if (!departmentExists)
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Avdeling med ID '{request.DepartmentId.Value}' ble ikke funnet."));
        }

        if (request.ManagerId.HasValue)
        {
            ApplicationUser? manager = await userRepository.GetByIdIgnoringFiltersAsync(
                request.ManagerId.Value, ct);

            if (manager is null || !manager.IsActive)
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet eller er inaktiv."));

            if (!departmentScope.IsAllowed(manager.DepartmentId, Permissions.UsersAll, Permissions.UsersReadSub))
                return Result<UserDto>.Failure(
                    AppError.Create(ErrorCode.Validation, 
                        "Du har ikke tilgang til å sette denne brukeren som leder."));
        }

        // Valider at stillingstittelen eksisterer hvis JobTitleId er angitt
        if (request.JobTitleId.HasValue)
        {
            bool jobTitleExists = await jobTitleRepository.ExistsAsync(
                jt => jt.Id == request.JobTitleId.Value && jt.IsActive, ct);
            if (!jobTitleExists)
                return Result<UserDto>.Failure(
                    AppError.NotFound($"Stillingstittel med ID '{request.JobTitleId.Value}' ble ikke funnet."));
        }

        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;
        if (request.EmploymentType.HasValue) user.EmploymentType = request.EmploymentType.Value;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        // Normaliserer og oppdater brukernavn da det endres ved epost bytte
        if (request.Email is not null)
        {
            user.Email = request.Email;
            user.NormalizedEmail = request.Email.ToUpperInvariant();
            user.UserName = request.Email;
            user.NormalizedUserName = request.Email.ToUpperInvariant();
        }

        if (request.JobTitleId.HasValue)
            user.JobTitleId = request.JobTitleId;
        else if (request.ClearJobTitleId)
            user.JobTitleId = null;

        if (request.DepartmentId.HasValue)
            user.DepartmentId = request.DepartmentId.Value;

        if (request.ManagerId.HasValue)
            user.ManagerId = request.ManagerId;
        else if (request.ClearManagerId)
            user.ManagerId = null;

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            if (request.Roles is not null)
            {
                IList<string> currentRoles = await userManager.GetRolesAsync(user);

                IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                    return Result<UserDto>.Failure(
                        AppError.Create(ErrorCode.InternalError, "Kunne ikke fjerne eksisterende roller."));

                if (request.Roles.Count > 0)
                {
                    IdentityResult addResult = await userManager.AddToRolesAsync(user, request.Roles);
                    if (!addResult.Succeeded)
                        return Result<UserDto>.Failure(
                            AppError.Create(ErrorCode.InternalError, "Kunne ikke tildele roller."));
                }
            }

            await userRepository.UpdateAsync(user, ct);
            await userRepository.SaveChangesAsync(ct);

            ApplicationUser? updatedUser = await userRepository.GetByIdWithDetailsAsync(userId, ct);
            if (updatedUser is null)
            {
                logger.LogError("Bruker {UserId} forsvant etter oppdatering", userId);
                return Result<UserDto>.Failure(
                    AppError.Create(ErrorCode.InternalError, "Brukeren ble ikke funnet etter oppdatering."));
            }

            logger.LogInformation("Bruker {UserId} oppdatert", userId);
            IList<string> roles = await userManager.GetRolesAsync(updatedUser);
            return Result<UserDto>.Success(UserMapper.ToDto(updatedUser, roles));
        }, ct);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserDto>>> GetPotentialManagersAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationUser> potentialManagers =
            await userRepository.GetPotentialManagersAsync(cancellationToken);

        var dtos = new List<UserDto>();
        foreach (ApplicationUser manager in potentialManagers)
        {
            IList<string> roles = await userManager.GetRolesAsync(manager);
            dtos.Add(UserMapper.ToDto(manager, roles));
        }

        return Result<IReadOnlyList<UserDto>>.Success(dtos);
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