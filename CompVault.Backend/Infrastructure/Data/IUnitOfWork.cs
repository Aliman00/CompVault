namespace CompVault.Backend.Infrastructure.Data;

/// <summary>
/// Abstraherer transaksjonsansvaret slik at service-laget kan samle flere
/// repository-operasjoner i én atomisk enhet før de persisteres til databasen.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persisterer alle ventende endringer i den gjeldende DbContext-instansen.
    /// Kall denne fra service-laget etter at alle repository-operasjoner er utført.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
