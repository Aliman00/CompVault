using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
namespace CompVault.Backend.Infrastructure.Repositories.Departments;

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
            .Include(d => d.CreatedBy)
            .Include(d => d.Manager)
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<Department>> GetAllWithHierarchyAsync(CancellationToken cancellationToken = default) =>
        await DbSet
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Include(d => d.ParentDepartment)
            .Include(d => d.SubDepartments)
            .Include(d => d.CreatedBy)
            .Include(d => d.Manager)
            .Where(d => d.IsActive && d.DeletedAt == null)
            .ToListAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasSubDepartmentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(d => d.ParentDepartmentId == id, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasMembersAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.DepartmentId == id && u.DeletedAt == null, cancellationToken);

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
    public Task SoftDeleteAsync(Department department)
    {
        department.DeletedAt = DateTime.UtcNow;
        department.IsActive = false;
        return Task.CompletedTask;
    }
}