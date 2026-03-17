using System.Linq.Expressions;
using CompVault.Backend.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Infrastructure.Repositories;

/// <summary>
/// EF Core-implementasjon av <see cref="IRepository{T}"/>.
/// Alle entitetsrepos arver herfra og kan overstyre metodene om nødvendig.
/// </summary>
/// <typeparam name="T">Entitetstypen.</typeparam>
public abstract class BaseRepository<T>(AppDbContext dbContext) : IRepository<T> where T : class
{
    protected readonly AppDbContext DbContext = dbContext;
    protected readonly DbSet<T> DbSet = dbContext.Set<T>();

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await DbSet.FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<T>> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    /// <inheritdoc />
    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbSet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        T? entity = await GetByIdAsync(id, cancellationToken);
        if (entity is not null)
        {
            DbSet.Remove(entity);
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(predicate, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken ct = default)
        => DbContext.SaveChangesAsync(ct);

    /// <summary>Gir tilgang til et IQueryable som kan bygges videre på før det kjøres mot DB.</summary>
    protected IQueryable<T> Query() => DbSet.AsQueryable();


}
