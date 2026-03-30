using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Shared.DTOs.Roles;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Roles.Services;

/// <summary>
/// Implementerer rolleadministrasjon.
/// </summary>
public sealed class RoleService(
    RoleManager<ApplicationRole> roleManager,
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    ICurrentUserProvider currentUserProvider,
    ILogger<RoleService> logger) : IRoleService
{

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<RoleDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        List<ApplicationRole> roles = await roleManager.Roles.ToListAsync(cancellationToken);
        if (roles.Count == 0)
            return Result<IReadOnlyList<RoleDto>>.Success([]);

        // Last brukerteller i bulk for å unngå N+1
        var roleIds = roles.Select(r => r.Id).ToList();
        Dictionary<Guid, int> userCounts = await dbContext.UserRoles
            .Where(ur => roleIds.Contains(ur.RoleId))
            .GroupBy(ur => ur.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

        // Last permissions i bulk for å unngå N+1
        Dictionary<Guid, List<string>> rolePermissions = await dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Include(rp => rp.Permission)
            .GroupBy(rp => rp.RoleId)
            .ToDictionaryAsync(g => g.Key, g => g.Select(rp => rp.Permission.Name).ToList(), cancellationToken);

        var roleDtos = roles
            .Select(role => RoleMapper.ToDto(
                role,
                userCounts.GetValueOrDefault(role.Id, 0),
                rolePermissions.GetValueOrDefault(role.Id, [])))
            .ToList();

        return Result<IReadOnlyList<RoleDto>>.Success(roleDtos);
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ApplicationRole? role = await roleManager.FindByIdAsync(id.ToString());
        if (role is null)
            return Result<RoleDto>.Failure(
                AppError.NotFound($"Rolle med ID '{id}' ble ikke funnet."));

        // Rolle skal alltid ha et navn etter opprettelse via RoleManager
        string roleName = role.Name ?? throw new InvalidOperationException(
            $"Rolle med ID '{id}' har null som navn. Dette skal ikke være mulig.");

        int userCount = (await userManager.GetUsersInRoleAsync(roleName)).Count;
        List<string> permissions = await GetPermissionNamesForRoleAsync(role.Id, cancellationToken);

        return Result<RoleDto>.Success(RoleMapper.ToDto(role, userCount, permissions));
    }

    /// <inheritdoc />
    public async Task<Result<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Forespørsel kan ikke være null."));

        bool exists = await roleManager.RoleExistsAsync(request.Name);
        if (exists)
            return Result<RoleDto>.Failure(
                AppError.Conflict($"En rolle med navn '{request.Name}' eksisterer allerede."));

        var role = new ApplicationRole
        {
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow,
            CreatedById = currentUserProvider.GetCurrentUserId()
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
        if (request is null)
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Forespørsel kan ikke være null."));

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

        // Rolle skal alltid ha et navn etter opprettelse via RoleManager
        string roleName = role.Name ?? throw new InvalidOperationException(
            $"Rolle med ID '{id}' har null som navn. Dette skal ikke være mulig.");

        int userCount = (await userManager.GetUsersInRoleAsync(roleName)).Count;
        List<string> permissions = await GetPermissionNamesForRoleAsync(role.Id, cancellationToken);

        return Result<RoleDto>.Success(RoleMapper.ToDto(role, userCount, permissions));
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

        // Rolle skal alltid ha et navn etter opprettelse via RoleManager
        string roleName = role.Name ?? throw new InvalidOperationException(
            $"Rolle med ID '{id}' har null som navn. Dette skal ikke være mulig.");

        int userCount = (await userManager.GetUsersInRoleAsync(roleName)).Count;
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
    public async Task<Result<RoleDto>> AssignPermissionsAsync(Guid roleId, AssignPermissionsRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Forespørsel kan ikke være null."));

        ApplicationRole? role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null)
            return Result<RoleDto>.Failure(
                AppError.NotFound($"Rolle med ID '{roleId}' ble ikke funnet."));

        // Valider at alle permissions finnes
        if (request.PermissionNames is null)
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Permission-navn kan ikke være null."));

        var requestedNames = request.PermissionNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<Permission> validPermissions = await dbContext.Permissions
            .Where(p => requestedNames.Contains(p.Name))
            .ToListAsync(cancellationToken);

        if (validPermissions.Count != requestedNames.Count)
        {
            var foundNames = validPermissions.Select(p => p.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> invalidNames = requestedNames.Except(foundNames, StringComparer.OrdinalIgnoreCase);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.Validation, $"Ugyldige permissions: {string.Join(", ", invalidNames)}"));
        }

        // Slett eksisterende role permissions
        List<RolePermission> existingPermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync(cancellationToken);

        // Bruk transaksjon for å sikre at vi ikke mister permissions hvis add feiler
        await using Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            dbContext.RolePermissions.RemoveRange(existingPermissions);

            // Opprett nye role permissions
            Guid? grantedById = currentUserProvider.GetCurrentUserId();
            var newRolePermissions = validPermissions.Select(p => new RolePermission
            {
                RoleId = roleId,
                PermissionId = p.Id,
                GrantedAt = DateTime.UtcNow,
                GrantedById = grantedById
            }).ToList();

            dbContext.RolePermissions.AddRange(newRolePermissions);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Feil ved tildeling av permissions til rolle {RoleId}", roleId);
            await transaction.RollbackAsync(cancellationToken);
            return Result<RoleDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Kunne ikke tildele permissions."));
        }

        // Rolle skal alltid ha et navn etter opprettelse via RoleManager
        string roleName = role.Name ?? throw new InvalidOperationException(
            $"Rolle med ID '{roleId}' har null som navn. Dette skal ikke være mulig.");

        int userCount = (await userManager.GetUsersInRoleAsync(roleName)).Count;
        var permissionNames = validPermissions.Select(p => p.Name).ToList();

        return Result<RoleDto>.Success(RoleMapper.ToDto(role, userCount, permissionNames));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<PermissionDto>>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
    {
        List<Permission> permissions = await dbContext.Permissions
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var dtos = permissions
            .Select(RoleMapper.ToPermissionDto)
            .ToList();

        return Result<IReadOnlyList<PermissionDto>>.Success(dtos);
    }

    private async Task<List<string>> GetPermissionNamesForRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        return await dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Include(rp => rp.Permission)
            .Select(rp => rp.Permission.Name)
            .ToListAsync(cancellationToken);
    }
}