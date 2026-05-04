using System.Text.Json;

using CompVault.Backend.Domain.Entities.Audit;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Identity;

namespace CompVault.Backend.Features.Roles.Services;

/// <summary>
/// Implementerer rolleadministrasjon.
/// </summary>
public sealed class RoleService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IRoleRepository roleRepository,
    IUnitOfWork unitOfWork,
    AppDbContext dbContext,
    ILogger<RoleService> logger) : IRoleService
{

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ApplicationRole> roles = await roleRepository.GetAllWithPermissionsAsync(cancellationToken);
        if (roles.Count == 0)
            return Result<IReadOnlyList<RoleDto>>.Success([]);

        // Last brukerteller i bulk for å unngå N+1
        var roleIds = roles.Select(r => r.Id).ToList();

        Dictionary<Guid, int> userCounts = await roleRepository.GetUserCountsForRolesAsync(roleIds, cancellationToken);

        var roleDtos = roles
            .Select(role => RoleMapper.ToDto(
                role,
                userCounts.GetValueOrDefault(role.Id, 0),
                role.RolePermissions.Select(rp => rp.Permission.Name).ToList()))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(roleDtos);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationRole? role = await roleRepository.GetByIdWithCreatedByAsync(id, cancellationToken);
        if (role is null)
            return Result<RoleDto>.Failure(
                AppError.NotFound($"Rolle med ID '{id}' ble ikke funnet."));

        int userCount = (await roleRepository.GetUserCountsForRolesAsync([role.Id], cancellationToken))
            .GetValueOrDefault(role.Id, 0);
        IReadOnlyList<string> permissions = await roleRepository.GetPermissionNamesForRoleAsync(role.Id, cancellationToken);

        return Result<RoleDto>.Success(RoleMapper.ToDto(role, userCount, permissions.ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, Guid createdById, CancellationToken cancellationToken = default)
    {
        bool exists = await roleManager.RoleExistsAsync(request.Name);
        if (exists)
            return Result<RoleDto>.Failure(
                AppError.Conflict($"En rolle med navn '{request.Name}' eksisterer allerede."));

        ApplicationUser? createdBy = await userManager.FindByIdAsync(createdById.ToString());
        if (createdBy is null)
        {
            logger.LogError("Bruker med ID {UserId} eksisterer ikke", createdById);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.UserNotFound, $"Bruker med ID '{createdById}' ble ikke funnet."));
        }

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedById = createdById,
            CreatedBy = createdBy
        };

        IdentityResult result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Kunne ikke opprette rolle {Name}: {Errors}", request.Name, errors);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke opprette rollen."));
        }

        return Result<RoleDto>.Success(RoleMapper.ToDto(role, 0, new List<string>()));
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        ApplicationRole? role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return Result<RoleDto>.Failure(
                AppError.NotFound($"Rolle med ID '{id}' ble ikke funnet."));

        if (request.Name is not null && !string.Equals(role.Name, request.Name, StringComparison.OrdinalIgnoreCase))
        {
            // Forhindre endring av systemrollenavn
            if (role.IsSystem)
                return Result<RoleDto>.Failure(
                    AppError.Conflict("Kan ikke endre navn på systemroller."));

            bool nameExists = await roleManager.RoleExistsAsync(request.Name);
            if (nameExists)
                return Result<RoleDto>.Failure(
                    AppError.Conflict($"En rolle med navn '{request.Name}' eksisterer allerede."));

            role.Name = request.Name;
        }

        if (request.Description is not null)
            role.Description = request.Description;

        IdentityResult result = await roleManager.UpdateAsync(role);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Kunne ikke oppdatere rolle {RoleId}: {Errors}", id, errors);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke oppdatere rollen."));
        }

        ApplicationRole? savedRole = (await roleRepository.GetByIdWithCreatedByAsync(role.Id, cancellationToken));
        int userCount = (await roleRepository.GetUserCountsForRolesAsync([role.Id], cancellationToken))
            .GetValueOrDefault(role.Id, 0);
        IReadOnlyList<string> permissions = await roleRepository.GetPermissionNamesForRoleAsync(role.Id, cancellationToken);

        return Result<RoleDto>.Success(RoleMapper.ToDto(savedRole!, userCount, permissions.ToList()));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationRole? role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Rolle med ID '{id}' ble ikke funnet."));

        if (role.IsSystem)
            return Result<bool>.Failure(
                AppError.Conflict("Kan ikke slette systemroller (Admin, Employee)."));

        int userCount = (await roleRepository.GetUserCountsForRolesAsync([role.Id], cancellationToken))
            .GetValueOrDefault(role.Id, 0);
        if (userCount > 0)
            return Result<bool>.Failure(
                AppError.Conflict($"Kan ikke slette en rolle som har {userCount} brukere tilknyttet."));

        IdentityResult result = await roleManager.DeleteAsync(role);
        if (!result.Succeeded)
        {
            string errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogWarning("Kunne ikke slette rolle {RoleId}: {Errors}", id, errors);
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke slette rollen."));
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> AssignPermissionsAsync(Guid roleId, AssignPermissionsRequest request, Guid grantedById, CancellationToken cancellationToken = default)
    {
        ApplicationRole? role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result<RoleDto>.Failure(
                AppError.NotFound($"Rolle med ID '{roleId}' ble ikke funnet."));

        if (role.IsSystem)
            return Result<RoleDto>.Failure(
                AppError.Conflict("Kan ikke endre permissions på systemroller."));

        if (request.PermissionNames is null)
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Permission-navn kan ikke være null."));

        var requestedNames = request.PermissionNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<Permission> validPermissions = await roleRepository.GetPermissionsByNamesAsync(requestedNames, cancellationToken);

        if (validPermissions.Count != requestedNames.Count)
        {
            var foundNames = validPermissions.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> invalidNames = requestedNames.Except(foundNames, StringComparer.OrdinalIgnoreCase);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, $"Ugyldige permissions: {string.Join(", ", invalidNames)}"));
        }

        return await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            // Hent eksisterende permissions før sletting for å logge hva som ble lagt til/fjernet
            IReadOnlyList<Permission> oldPermissions = await roleRepository.GetPermissionsByNamesAsync(
                await roleRepository.GetPermissionNamesForRoleAsync(roleId, cancellationToken) is { } names
                    ? names.ToHashSet(StringComparer.OrdinalIgnoreCase)
                    : [], cancellationToken);
            var oldPermissionNames = oldPermissions.Select(p => p.Name).ToList();

            await roleRepository.RemoveRolePermissionsAsync(roleId, cancellationToken);

            var newRolePermissions = validPermissions.Select(p => new RolePermission
            {
                RoleId = roleId,
                PermissionId = p.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedById = grantedById
            }).ToList();

            await roleRepository.AddRolePermissionsAsync(newRolePermissions, cancellationToken);

            // Opprett revisjonslogg for tillatelsestildeling
            var addedPermissions = validPermissions.Select(p => p.Name).Except(oldPermissionNames).ToList();
            var removedPermissions = oldPermissionNames.Except(validPermissions.Select(p => p.Name)).ToList();

            // Hent innlogget bruker for audit
            string? userName = null;
            string? userEmail = null;
            ApplicationUser? grantedByUser = await userManager.FindByIdAsync(grantedById.ToString());
            if (grantedByUser is not null)
            {
                userName = $"{grantedByUser.FirstName} {grantedByUser.LastName}";
                userEmail = grantedByUser.Email;
            }

            dbContext.AuditLogs.Add(new AuditLog
            {
                Action = "role.permissions_assigned",
                EntityType = "ApplicationRole",
                EntityId = roleId,
                UserId = grantedById,
                UserName = userName,
                UserEmail = userEmail,
                Details = JsonSerializer.Serialize(new
                {
                    added_permissions = addedPermissions,
                    removed_permissions = removedPermissions,
                    role_name = role.Name
                }),
            });

            int userCount = (await roleRepository.GetUserCountsForRolesAsync([roleId], cancellationToken))
                .GetValueOrDefault(roleId, 0);
            var permissionNames = validPermissions.Select(p => p.Name).ToList();

            return Result<RoleDto>.Success(RoleMapper.ToDto(role, userCount, permissionNames));
        }, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Permission> permissions = await roleRepository.GetAllPermissionsAsync(cancellationToken);

        var dtos = permissions
            .Select(RoleMapper.ToPermissionDto)
            .ToList();

        return Result<IReadOnlyList<PermissionDto>>.Success(dtos);
    }
}