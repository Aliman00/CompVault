using System.Linq.Expressions;

namespace CompVault.Backend.Infrastructure.Repositories;

/// <summary>
/// Generisk repository-interface med standard CRUD for alle entiteter.
/// Alle konkrete repositories arver fra denne.
/// </summary>
/// <typeparam name="T">Entitetstypen.</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>Henter én entitet basert på ID.</summary>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alle entiteter som en read-only liste.</summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Henter en filtrert liste basert på et predikat.</summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Legger til en ny entitet i change-trackeren. Kall SaveChangesAsync() for å persistere.</summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Markerer en entitet som endret i change-trackeren. Kall SaveChangesAsync() for å persistere.</summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>Markerer en entitet for sletting. Kall SaveChangesAsync() for å persistere.</summary>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om det finnes noen entitet som matcher predikatet.</summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>Persisterer alle ventende endringer til databasen</summary>
    Task SaveChangesAsync(CancellationToken ct = default);
}
