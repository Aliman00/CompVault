using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories.Identity;

/// <summary>
/// EF Core-implementasjon av <see cref="IDepartmentRepository"/>.
/// </summary>
public sealed class DepartmentRepository(AppDbContext dbContext) : BaseRepository<Department>(dbContext), IDepartmentRepository
{
    /// <inheritdoc />
    public async Task<Department?> GetByIdWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.SubDepartments)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Department>> GetAllWithHierarchyAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.SubDepartments)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasSubDepartmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(d => d.ParentDepartmentId == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasMembersAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbContext.Users.AnyAsync(u => u.DepartmentId == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var allDepartments = await DbSet
            .AsNoTracking()
            .Select(d => new { d.Id, d.ParentDepartmentId })
            .ToListAsync(cancellationToken);

        var ancestorIds = new List<Guid>();
        var current = allDepartments.FirstOrDefault(d => d.Id == id);

        while (current?.ParentDepartmentId is not null)
        {
            ancestorIds.Add(current.ParentDepartmentId.Value);
            current = allDepartments.FirstOrDefault(d => d.Id == current.ParentDepartmentId.Value);
        }

        return ancestorIds;
    }

    /// <inheritdoc />
    public async Task SoftDeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        department.DeletedAt = DateTime.UtcNow;
        department.IsActive = false;
        await SaveChangesAsync(cancellationToken);
    }
}
